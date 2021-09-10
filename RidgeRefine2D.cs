using System;
using System.Collections.Generic;
using System.Numerics;

namespace Arihara.GuideSmoke
{
  struct AdjacencyList
  {
    public int ix, iy, iz;
    public List<AdjacencyList> lists;
    public Vector3 pos;
  }

  /*
    Refer: Florian,F. etal., Interacive Separating Streak Surfaces,(2010)
    https://ieeexplore.ieee.org/document/5613499
  */
  class RidgeRefine2D
  {
    bool[,,] region;
    float[,,] ftle;
    Vector3[,,] positions;
    Vector3[,,] gradients;
    List<AdjacencyList> headLists;
    int lenX, lenY, lenZ;
    float deltaX, deltaY, deltaZ;

    #region Paramters
    float delta = 0.5f;
    float sigma = 0.25f;
    float omega = 0.05f;
    #endregion


    public RidgeRefine(bool[,,] r, float[,,] f, Vector3[,,] p, int lX, int lY, int lZ, float dx, float dy, float dz)
    {
      this.region = r;
      this.ftle = f;
      this.positions = p;
      this.lenX = lX;
      this.lenY = lY;
      this.lenZ = lZ;
      this.deltaX = dx;
      this.deltaY = dy;
      this.deltaZ = dz;
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
        if (lenZ != 1) continue;
        if (adj.lists.Count == 1)
        {
          int ix = adj.ix;
          int iy = adj.iy;
          int iz = adj.iz;
          Vector2 nv = UnitLengthPerpendicular2D(new Vector2(adj.pos.X, adj.pos.Y));
          Vector2 g = new Vector2(gradients[ix, iy, iz].X, gradients[ix, iy, iz].Y);
          Vector2 rv = Vector2.Dot(nv, g) * nv;
          Vector2 p = new Vector2(positions[ix, iy, iz].X, positions[ix, iy, iz].Y);
          new_positions.Add((ix, iy), p + rv * this.delta);
        }
        else
        {
        }

        foreach (var np in new_positions)
        {
          (int ix, int iy) index = np.Key;
          positions[index.ix, index.iy, 0] = np.Value;
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
          for (int iz = 0; iz < lenZ; iz++)
          {
            if (ix == 0 || iy == 0 || iz == 0 || ix == lenX - 1 || iy == lenY - 1 || iz == lenZ - 1)
            {
              gradients[ix, iy, iz] = Vector3.Zero;
            }
            else
            {
              float dx = (ftle[ix + 1, iy, iz] - ftle[ix - 1, iy, iz]) / (2 * deltaX);
              float dy = (ftle[ix, iy + 1, iz] - ftle[ix, iy - 1, iz]) / (2 * deltaY);
              float dz = (ftle[ix, iy, iz + 1] - ftle[ix, iy, iz - 1]) / (2 * deltaZ);
              gradients[ix, iy, iz] = new Vector3(dx, dy, dz);
            }
          }
        }
      }
    }

    private void ConstructAdjList()
    {
      bool[,,] isAddedGraph = new bool[lenX, lenY, lenZ];
      headLists = new List<AdjacencyList>();
      for (int ix = 0; ix < lenX; ix++)
      {
        for (int iy = 0; iy < lenY; iy++)
        {
          for (int iz = 0; iz < lenZ; iz++)
          {
            AdjacencyList head = new AdjacencyList();
            headLists.Add(head);
            SetValue(ref head, ix, iy, iz);
          }
        }
      }

      void SetValue(ref AdjacencyList al, int ix, int iy, int iz)
      {
        bool N = false;
        bool W = false;
        bool S = false;
        bool E = false;
        Vector3 p = positions[ix, iy, iz];
        al.pos = new Vector3(p.X, p.Y, p.Z);
        al.lists = new List<AdjacencyList>();
        al.ix = ix;
        al.iy = iy;
        al.iz = iz;
        isAddedGraph[ix, iy, iz] = true;

        /* West */
        if (0 < ix)
        {
          if (region[ix - 1, iy, iz] && !isAddedGraph[ix - 1, iy, iz])
          {
            AdjacencyList w = new AdjacencyList();
            al.lists.Add(w);
            SetValue(ref w, ix - 1, iy, iz);
            W = true;
          }
        }

        /* East */
        if (ix < lenX - 1)
        {
          if (region[ix + 1, iy, iz] && !isAddedGraph[ix + 1, iy, iz])
          {
            AdjacencyList e = new AdjacencyList();
            al.lists.Add(e);
            SetValue(ref e, ix + 1, iy, iz);
            E = true;
          }
        }

        /* North */
        if (0 < iy)
        {
          if (region[ix, iy - 1, iz] && !isAddedGraph[ix, iy - 1, iz])
          {
            AdjacencyList n = new AdjacencyList();
            al.lists.Add(n);
            SetValue(ref n, ix, iy - 1, iz);
            N = true;
          }
        }

        /* South */
        if (iy < lenY - 1)
        {
          if (region[ix, iy + 1, iz] && !isAddedGraph[ix, iy + 1, iz])
          {
            AdjacencyList s = new AdjacencyList();
            al.lists.Add(s);
            SetValue(ref s, ix, iy + 1, iz);
            S = true;
          }
        }

        /* NW */
        if (!(N || W) && 0 < iy && 0 < ix)
        {
          if (region[ix - 1, iy - 1, iz] && !isAddedGraph[ix - 1, iy - 1, iz])
          {
            AdjacencyList nw = new AdjacencyList();
            al.lists.Add(nw);
            SetValue(ref nw, ix - 1, iy - 1, iz);
          }
        }

        /* NE */
        if (!(N || E) && 0 < iy && ix < lenX - 1)
        {
          if (region[ix + 1, iy - 1, iz] && !isAddedGraph[ix + 1, iy - 1, iz])
          {
            AdjacencyList ne = new AdjacencyList();
            al.lists.Add(ne);
            SetValue(ref ne, ix + 1, iy - 1, iz);
          }
        }

        /* SW */
        if (!(S || W) && iy < lenY - 1 && 0 < ix)
        {
          if (region[ix - 1, iy + 1, iz] && !isAddedGraph[ix - 1, iy + 1, iz])
          {
            AdjacencyList sw = new AdjacencyList();
            al.lists.Add(sw);
            SetValue(ref sw, ix - 1, iy + 1, iz);
          }
        }

        /* SE */
        if (!(S || E) && iy < lenY - 1 && ix < lenX - 1)
        {
          if (region[ix + 1, iy + 1, iz] && !isAddedGraph[ix + 1, iy + 1, iz])
          {
            AdjacencyList se = new AdjacencyList();
            al.lists.Add(se);
            SetValue(ref se, ix + 1, iy + 1, iz);
          }
        }
      }
    }
  }
}