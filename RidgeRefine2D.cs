using System;
using System.Collections.Generic;
using System.Numerics;

namespace Arihara.GuideSmoke
{
  struct Pixel
  {
    public bool isInvalid;
    public int ix, iy;
    public List<Pixel> adjacents;
    public Vector2 pos;
    public float dv;
  }

  /*
    Refer: Florian,F. etal., Interacive Separating Streak Surfaces,(2010)
    https://ieeexplore.ieee.org/document/5613499
  */
  class RidgeRefine2D
  {
    bool[,] region;
    float[,] ftle;
    Vector2[,] positions;
    Vector2[,] gradients;
    Pixel[,] pixels;
    int lenX, lenY;
    float deltaX, deltaY;

    #region Paramters
    float delta = 0.5f;
    float sigma = 0.25f;
    float omega = 0.05f;
    float d_max;
    int k_cut = 5;
    #endregion


    public RidgeRefine2D(bool[,] r, float[,] f, Vector2[,] p, int lX, int lY, float dx, float dy)
    {
      this.region = r;
      this.ftle = f;
      this.positions = p;
      this.lenX = lX;
      this.lenY = lY;
      this.deltaX = dx;
      this.deltaY = dy;
    }

    public void SetParameters(float d, float s, float o, float d_m, int k)
    {
      this.delta = d;
      this.sigma = s;
      this.omega = o;
      this.d_max = d_m;
      this.k_cut = k;
    }

    public void SubPixelRidgeRefinement()
    {
      ConstructAdjList();
      ComputeFTLEGradient();
      Refinements();
    }

    private void Refinements()
    {
      Dictionary<(int, int), Vector2> new_positions = new Dictionary<(int, int), Vector2>();
      for (int ix = 0; ix < lenX; ix++)
      {
        for (int iy = 0; iy < lenY; iy++)
        {
          Pixel you = pixels[ix, iy];
          if(you.isInvalid) continue;
          if (you.adjacents.Count == 1)
          {
            Pixel adjacent = you.adjacents[0];
            Vector2 nv = UnitLengthPerpendicular2D(adjacent.pos - you.pos);
            Vector2 g = gradients[adjacent.ix, adjacent.iy];
            Vector2 rv = Vector2.Dot(nv, g) * nv;
            Vector2 new_pv = you.pos + this.delta * rv;
            new_positions.Add((ix, iy), new_pv);
          }
          else
          {
            Vector2 rv = Vector2.Zero;
            Vector2 pu = Vector2.Zero;
            foreach (Pixel adjacent in you.adjacents)
            {
              Vector2 nv = UnitLengthPerpendicular2D(adjacent.pos - you.pos);
              Vector2 g = gradients[adjacent.ix, adjacent.iy];
              rv += Vector2.Dot(nv, g) * nv;
              pu += adjacent.pos;
            }
            Vector2 new_pv = (1 - this.sigma) * you.pos + this.sigma * pu / you.adjacents.Count + this.delta * rv;
            new_positions.Add((ix, iy), new_pv);
          }

          foreach (var np in new_positions)
          {
            (int ix, int iy) index = np.Key;
            positions[index.ix, index.iy] = np.Value;
          }
        }
      }

      Vector2 UnitLengthPerpendicular2D(Vector2 v)
      {
        float a = v.X;
        float b = v.Y;
        float k2 = (float)Math.Sqrt((a * a) / (a * a + b * b));
        float k1 = -b * k2 / a;
        return new Vector2(k1, k2);
      }

      void PostProcess()
      {

      }
    }

    private void ComputeFTLEGradient()
    {
      for (int ix = 0; ix < lenX; ix++)
      {
        for (int iy = 0; iy < lenY; iy++)
        {
          if (ix == 0 || iy == 0 || ix == lenX - 1 || iy == lenY - 1)
          {
            gradients[ix, iy] = Vector2.Zero;
          }
          else
          {
            float dx = (ftle[ix + 1, iy] - ftle[ix - 1, iy]) / (2 * deltaX);
            float dy = (ftle[ix, iy + 1] - ftle[ix, iy - 1]) / (2 * deltaY);
            gradients[ix, iy] = new Vector2(dx, dy);
          }
        }
      }
    }

    private void ConstructAdjList()
    {
      bool[,] isAddedPixels = new bool[lenX, lenY];
      pixels = new Pixel[lenX, lenY];
      for (int ix = 0; ix < lenX; ix++)
      {
        for (int iy = 0; iy < lenY; iy++)
        {
          if (region[ix, iy] && !isAddedPixels[ix, iy]) pixels[ix, iy] = new Pixel();
          SetValue(ref pixels[ix, iy], ix, iy);
        }
      }

      void SetValue(ref Pixel px, int ix, int iy)
      {
        bool N = false;
        bool W = false;
        bool S = false;
        bool E = false;
        Vector2 p = positions[ix, iy];
        px.pos = new Vector2(p.X, p.Y);
        px.adjacents = new List<Pixel>();
        px.isInvalid = false;
        px.ix = ix;
        px.iy = iy;
        px.dv = 0;
        isAddedPixels[ix, iy] = true;

        /* West */
        if (0 < ix)
        {
          if (region[ix - 1, iy])
          {
            if (!isAddedPixels[ix - 1, iy])
            {
              pixels[ix - 1, iy] = new Pixel();
              isAddedPixels[ix - 1, iy] = true;
            }
            px.adjacents.Add(pixels[ix - 1, iy]);
            W = true;
          }
        }

        /* East */
        if (ix < lenX - 1)
        {
          if (region[ix + 1, iy])
          {
            if (!isAddedPixels[ix + 1, iy])
            {
              pixels[ix + 1, iy] = new Pixel();
              isAddedPixels[ix + 1, iy] = true;
            }
            px.adjacents.Add(pixels[ix + 1, iy]);
            E = true;
          }
        }

        /* North */
        if (0 < iy)
        {
          if (region[ix, iy - 1])
          {
            if (!isAddedPixels[ix, iy - 1])
            {
              pixels[ix, iy - 1] = new Pixel();
              isAddedPixels[ix, iy - 1] = true;
            }
            px.adjacents.Add(pixels[ix, iy - 1]);
            N = true;
          }
        }

        /* South */
        if (iy < lenY - 1)
        {
          if (region[ix, iy + 1])
          {
            if (!isAddedPixels[ix, iy + 1])
            {
              pixels[ix, iy + 1] = new Pixel();
              isAddedPixels[ix, iy + 1] = true;
            }
            px.adjacents.Add(pixels[ix, iy + 1]);
            S = true;
          }
        }

        /* NW */
        if (!(N || W) && 0 < iy && 0 < ix)
        {
          if (region[ix - 1, iy - 1])
          {
            if (!isAddedPixels[ix - 1, iy - 1])
            {
              pixels[ix - 1, iy - 1] = new Pixel();
              isAddedPixels[ix - 1, iy - 1] = true;
            }
            px.adjacents.Add(pixels[ix - 1, iy - 1]);
          }
        }

        /* NE */
        if (!(N || E) && 0 < iy && ix < lenX - 1)
        {
          if (region[ix + 1, iy - 1])
          {
            if (!isAddedPixels[ix + 1, iy - 1])
            {
              pixels[ix + 1, iy - 1] = new Pixel();
              isAddedPixels[ix + 1, iy - 1] = true;
            }
            px.adjacents.Add(pixels[ix + 1, iy - 1]);
          }
        }

        /* SW */
        if (!(S || W) && iy < lenY - 1 && 0 < ix)
        {
          if (region[ix - 1, iy + 1])
          {
            if (!isAddedPixels[ix - 1, iy + 1])
            {
              pixels[ix - 1, iy + 1] = new Pixel();
              isAddedPixels[ix - 1, iy + 1] = true;
            }
            px.adjacents.Add(pixels[ix - 1, iy + 1]);
          }
        }

        /* SE */
        if (!(S || E) && iy < lenY - 1 && ix < lenX - 1)
        {
          if (region[ix + 1, iy + 1])
          {
            if (!isAddedPixels[ix + 1, iy + 1])
            {
              pixels[ix + 1, iy + 1] = new Pixel();
              isAddedPixels[ix + 1, iy + 1] = true;
            }
            px.adjacents.Add(pixels[ix + 1, iy + 1]);
          }
        }
      }
    }
  }
}