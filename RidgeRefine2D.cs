using System;
using System.Collections.Generic;
using System.Numerics;

namespace Arihara.GuideSmoke
{
  struct AdjacencyList
  {
    public int ix, iy;
    public List<AdjacencyList> lists;
    public Vector2 pos;
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
    List<AdjacencyList> headLists;
    int lenX, lenY;
    float deltaX, deltaY;

    #region Paramters
    float delta = 0.5f;
    float sigma = 0.25f;
    float omega = 0.05f;
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

    public void SetParameters(float d, float s, float o)
    {
      this.delta = d;
      this.sigma = s;
      this.omega = o;
    }

    public void SubPixelRidgeRefinement()
    {
      ConstructAdjList();
      ComputeFTLEGradient();
    }

    private void Refinements()
    {
      Dictionary<(int, int), Vector2> new_positions = new Dictionary<(int, int), Vector2>();
      foreach (var adj in headLists)
      {
        if (adj.lists.Count == 1)
        {
          int ix = adj.ix;
          int iy = adj.iy;
          Vector2 nv = UnitLengthPerpendicular2D(adj.pos);
          Vector2 g = gradients[ix, iy];
          Vector2 rv = Vector2.Dot(nv, g) * nv;
          Vector2 p = positions[ix, iy];
          new_positions.Add((ix, iy), p + rv * this.delta);
        }
        else
        {
        }

        foreach (var np in new_positions)
        {
          (int ix, int iy) index = np.Key;
          positions[index.ix, index.iy] = np.Value;
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
      bool[,] isAddedGraph = new bool[lenX, lenY];
      headLists = new List<AdjacencyList>();
      for (int ix = 0; ix < lenX; ix++)
      {
        for (int iy = 0; iy < lenY; iy++)
        {
          AdjacencyList head = new AdjacencyList();
          headLists.Add(head);
          SetValue(ref head, ix, iy);
        }
      }

      void SetValue(ref AdjacencyList al, int ix, int iy)
      {
        bool N = false;
        bool W = false;
        bool S = false;
        bool E = false;
        Vector2 p = positions[ix, iy];
        al.pos = new Vector2(p.X, p.Y);
        al.lists = new List<AdjacencyList>();
        al.ix = ix;
        al.iy = iy;

        isAddedGraph[ix, iy] = true;

        /* West */
        if (0 < ix)
        {
          if (region[ix - 1, iy] && !isAddedGraph[ix - 1, iy])
          {
            AdjacencyList w = new AdjacencyList();
            al.lists.Add(w);
            SetValue(ref w, ix - 1, iy);
            W = true;
          }
        }

        /* East */
        if (ix < lenX - 1)
        {
          if (region[ix + 1, iy] && !isAddedGraph[ix + 1, iy])
          {
            AdjacencyList e = new AdjacencyList();
            al.lists.Add(e);
            SetValue(ref e, ix + 1, iy);
            E = true;
          }
        }

        /* North */
        if (0 < iy)
        {
          if (region[ix, iy - 1] && !isAddedGraph[ix, iy - 1])
          {
            AdjacencyList n = new AdjacencyList();
            al.lists.Add(n);
            SetValue(ref n, ix, iy - 1);
            N = true;
          }
        }

        /* South */
        if (iy < lenY - 1)
        {
          if (region[ix, iy + 1] && !isAddedGraph[ix, iy + 1])
          {
            AdjacencyList s = new AdjacencyList();
            al.lists.Add(s);
            SetValue(ref s, ix, iy + 1);
            S = true;
          }
        }

        /* NW */
        if (!(N || W) && 0 < iy && 0 < ix)
        {
          if (region[ix - 1, iy - 1] && !isAddedGraph[ix - 1, iy - 1])
          {
            AdjacencyList nw = new AdjacencyList();
            al.lists.Add(nw);
            SetValue(ref nw, ix - 1, iy - 1);
          }
        }

        /* NE */
        if (!(N || E) && 0 < iy && ix < lenX - 1)
        {
          if (region[ix + 1, iy - 1] && !isAddedGraph[ix + 1, iy - 1])
          {
            AdjacencyList ne = new AdjacencyList();
            al.lists.Add(ne);
            SetValue(ref ne, ix + 1, iy - 1);
          }
        }

        /* SW */
        if (!(S || W) && iy < lenY - 1 && 0 < ix)
        {
          if (region[ix - 1, iy + 1] && !isAddedGraph[ix - 1, iy + 1])
          {
            AdjacencyList sw = new AdjacencyList();
            al.lists.Add(sw);
            SetValue(ref sw, ix - 1, iy + 1);
          }
        }

        /* SE */
        if (!(S || E) && iy < lenY - 1 && ix < lenX - 1)
        {
          if (region[ix + 1, iy + 1] && !isAddedGraph[ix + 1, iy + 1])
          {
            AdjacencyList se = new AdjacencyList();
            al.lists.Add(se);
            SetValue(ref se, ix + 1, iy + 1);
          }
        }
      }
    }
  }
}