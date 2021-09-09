using System;

namespace Arihara.GuideSmoke
{
  delegate void LcsMethod(ref bool[,,] region, float[,,] ftle);

  struct AdjacencyList
  {
      
  }

  class LCS : IDisposable
  {
    private float[,,] fFTLE = null;
    private float[,,] bFTLE = null;
    private bool[,,] fRegion, bRegion;
    private int[,,] lcs;
    private int lenX, lenY, lenZ;
    private float deltaX, deltaY, deltaZ;
    private bool isComputable;

    #region Parameters
    private float threshold = 0.5f;
    private float kappa = 0.1f;
    #endregion

    #region Accessor
    public float[,,] ForwardFTLE
    {
      get { return fFTLE; }
    }
    public float[,,] BackwardFTLE
    {
      get { return bFTLE; }
    }
    public bool IsComputable
    {
      get { return isComputable; }
    }
    #endregion

    public LCS(float[,,] forwardFTLE, float[,,] backwardFTLE, int lenX, int lenY, int lenZ, float dx, float dy, float dz)
    {
      if (forwardFTLE != null) fFTLE = forwardFTLE;
      if (backwardFTLE != null) bFTLE = backwardFTLE;
      this.lenX = lenX;
      this.lenY = lenY;
      this.lenZ = lenZ;
      this.deltaX = dx;
      this.deltaY = dy;
      this.deltaZ = dz;
      lcs = new int[lenX, lenY, lenZ];

      if (fFTLE != null || bFTLE != null) isComputable = true;
      else isComputable = false;
    }

    public void Calculation(string methodName, int gaussianNum, int skeletonizeNum)
    {
      int methodNum = 0;
      LcsMethod lcsMethod;
      if (string.Equals(methodName, "Hessian")) methodNum = 1;
      if (string.Equals(methodName, "Threshold")) methodNum = 2;

      switch (methodNum)
      {
        case 1:
          lcsMethod = ByHessian;
          break;

        case 2:
          lcsMethod = BySimpleThreshold;
          break;

        default:
          Console.WriteLine($"{methodName} is not exist.");
          return;
      }

      if (fFTLE != null)
      {
        for (int i = 0; i < gaussianNum; i++)
        {
          GaussianFilter2D(ref fFTLE);
        }
        lcsMethod(ref fRegion, fFTLE);
        Skeletonization(ref fRegion, skeletonizeNum);
      }

      if (bFTLE != null)
      {
        for (int i = 0; i < gaussianNum; i++)
        {
          GaussianFilter2D(ref bFTLE);
        }
        lcsMethod(ref bRegion, bFTLE);
        Skeletonization(ref bRegion, skeletonizeNum);
      }

      for (int ix = 0; ix < lenX; ix++)
      {
        for (int iy = 0; iy < lenY; iy++)
        {
          for (int iz = 0; iz < lenZ; iz++)
          {
            if (fFTLE != null) lcs[ix, iy, iz] += (fRegion[ix, iy, iz]) ? 1 : 0;
            if (bFTLE != null) lcs[ix, iy, iz] += (bRegion[ix, iy, iz]) ? 1 : 0;
          }
        }
      }
    }

    public void SetParameters(float kp, float thre)
    {
      this.kappa = kp;
      this.threshold = thre;
    }

    public void BySimpleThreshold(ref bool[,,] region, float[,,] ftle)
    {
      Normalize(ref ftle);

      region = new bool[lenX, lenY, lenZ];
      float maxEigen = -100;
      for (int ix = 0; ix < lenX; ix++)
      {
        for (int iy = 0; iy < lenY; iy++)
        {
          if (maxEigen < ftle[ix, iy, 0]) maxEigen = ftle[ix, iy, 0];
        }
      }

      for (int ix = 0; ix < lenX; ix++)
      {
        for (int iy = 0; iy < lenY; iy++)
        {
          if ((maxEigen * this.threshold) < ftle[ix, iy, 0]) region[ix, iy, 0] = true;
          else region[ix, iy, 0] = false;
        }
      }
    }

    private void ByHessian(ref bool[,,] region, float[,,] ftle)
    {
      if (lenZ > 1)
      {
        Console.Write("Only supports 2D currently.");
        return;
      }
      else
      {
        region = new bool[lenX, lenY, lenZ];
        float[,,] secondDerivative = new float[lenX, lenY, 3]; //[lenX, lenY, (dx^2, dy^2, dxdy)]
        for (int ix = 0; ix < lenX; ix++)
        {
          for (int iy = 0; iy < lenY; iy++)
          {
            float dxx = (GetCoordValue(ftle, ix + 1, iy, 0) + GetCoordValue(ftle, ix - 1, iy, 0)
             - 2 * GetCoordValue(ftle, ix, iy, 0)) / (deltaX * deltaX);
            float dyy = (GetCoordValue(ftle, ix, iy + 1, 0) + GetCoordValue(ftle, ix, iy - 1, 0)
             - 2 * GetCoordValue(ftle, ix, iy, 0)) / (deltaY * deltaY);
            float dxdy = (GetCoordValue(ftle, ix + 1, iy + 1, 0) - GetCoordValue(ftle, ix - 1, iy + 1, 0)
                        - GetCoordValue(ftle, ix + 1, iy - 1, 0) + GetCoordValue(ftle, ix - 1, iy - 1, 0)) 
                        / (deltaX * deltaY);
            secondDerivative[ix, iy, 0] = dxx;
            secondDerivative[ix, iy, 1] = dyy;
            secondDerivative[ix, iy, 2] = dxdy;
          }
        }

        float[,] eigenValue = new float[lenX, lenY];
        for (int ix = 0; ix < lenX; ix++)
        {
          for (int iy = 0; iy < lenY; iy++)
          {
            float[,] hessian = new float[2, 2];
            hessian[0, 0] = secondDerivative[ix, iy, 0];
            hessian[1, 1] = secondDerivative[ix, iy, 1];
            hessian[0, 1] = hessian[1, 0] = secondDerivative[ix, iy, 2];
            eigenValue[ix, iy] = Eigen.GetMaxEigenValue2x2(hessian);
          }
        }

        float maxEigen = -100;
        float maxFTLE = -100;
        for (int ix = 0; ix < lenX; ix++)
        {
          for (int iy = 0; iy < lenY; iy++)
          {
            if (ftle[ix, iy, 0] > maxFTLE) maxFTLE = ftle[ix, iy, 0];
            if (eigenValue[ix, iy] > maxEigen) maxEigen = eigenValue[ix, iy];
          }
        }

        for (int ix = 0; ix < lenX; ix++)
        {
          for (int iy = 0; iy < lenY; iy++)
          {
            if (eigenValue[ix, iy] <= (this.kappa * maxEigen) && (maxFTLE * this.threshold) <= ftle[ix, iy, 0]) region[ix, iy, 0] = true;
            else region[ix, iy, 0] = false;
          }
        }
      }
    }

    private void Skeletonization(ref bool[,,] region, int iteration)
    {
      const int N = 0b0001;
      const int E = 0b0010;
      const int S = 0b0100;
      const int W = 0b1000;
      const int NW = N | W;
      const int SE = S | E;
      const int NE = N | E;
      const int SW = S | W;

      int[] dir = { NW, SE, NE, SW };
      int[,] Template1 = {
        {  0,  0,  0},
        {  2,  1,  2},
        {  2,  1,  2},
      };
      int[,] Template2 = {
        {  0,  2,  2},
        {  0,  1,  1},
        {  0,  2,  2},
      };
      int[,] Template3 = {
        {  0,  0, -1},
        {  0,  1,  1},
        { -1,  1, -1},
      };

      for (int n = 1; n < iteration; n++)
      {
        int[,] bounderPixel = new int[lenX, lenY];
        for (int ix = 1; ix < (lenX - 1); ix++)
        {
          for (int iy = 1; iy < (lenY - 1); iy++)
          {
            if (!region[ix, iy, 0]) continue;
            if (!region[ix, iy + 1, 0]) bounderPixel[ix, iy] |= N;
            if (!region[ix, iy - 1, 0]) bounderPixel[ix, iy] |= S;
            if (!region[ix + 1, iy, 0]) bounderPixel[ix, iy] |= E;
            if (!region[ix - 1, iy, 0]) bounderPixel[ix, iy] |= W;
          }
        }

        /* i = 0:NW, 1:SE, 2:NE, 3:SW */
        for (int i = 0; i < 4; i++)
        {
          for (int ix = 0; ix < lenX; ix++)
          {
            for (int iy = 0; iy < lenY; iy++)
            {
              if ((bounderPixel[ix, iy] & dir[i]) == 0) continue;
              if (isMatchTemplate(region, ix, iy, Rotation(i, Template1)))
              {
                region[ix, iy, 0] = false;
                continue;
              }

              if (isMatchTemplate(region, ix, iy, Rotation(i, Template2)))
              {
                region[ix, iy, 0] = false;
                continue;
              }

              if (isMatchTemplate(region, ix, iy, Rotation(i, Template3)))
              {
                region[ix, iy, 0] = false;
                continue;
              }
            }
          }
        }
      }

      bool isMatchTemplate(bool[,,] region, int cx, int cy, int[,] template)
      {
        bool isWildcardX = false;
        for (int ix = -1; ix < 2; ix++)
        {
          for (int iy = -1; iy < 2; iy++)
          {
            int x = cx + ix;
            int y = cy + iy;
            int templateNum = template[ix + 1, iy + 1];
            switch (templateNum)
            {
              case 0:
                if (region[x, y, 0] == false) continue;
                else return false;
              case 1:
                if (region[x, y, 0] == true) continue;
                else return false;

              case 2:
                if (region[x, y, 0] == true) isWildcardX = true;
                continue;

              default:
                continue;
            }
          }
        }
        return isWildcardX;
      }

      int[,] Rotation(int num, int[,] src)
      {
        int[,] dst = src;

        for (int i = 0; i < num; i++)
        {
          int[,] tmp = new int[lenX, lenY];
          tmp[2, 0] = dst[0, 0];
          tmp[1, 0] = dst[0, 1];
          tmp[0, 0] = dst[0, 2];
          tmp[2, 1] = dst[1, 0];
          tmp[1, 1] = dst[1, 1];
          tmp[0, 1] = dst[1, 2];
          tmp[2, 2] = dst[2, 0];
          tmp[1, 2] = dst[2, 1];
          tmp[0, 2] = dst[2, 2];
          dst = tmp;
        }
        return dst;
      }
    }

    /*
      Refer: Florian,F. etal., Interacive Separating Streak Surfaces,(2010)
      https://ieeexplore.ieee.org/document/5613499
    */
    private void SubPixelRigdeRefinement(bool[,,] region, float[,,] ftle)
    {
      
    }

    private void Normalize(ref float[,,] ftle)
    {
      float min = ftle[0, 0, 0];
      float max = min;
      for (int ix = 0; ix < lenX; ix++)
      {
        for (int iy = 0; iy < lenY; iy++)
        {
          for (int iz = 0; iz < lenZ; iz++)
          {
            float num = ftle[ix, iy, iz];
            if (num < min) min = num;
            if (max < num) max = num;
          }
        }
      }

      for (int ix = 0; ix < lenX; ix++)
      {
        for (int iy = 0; iy < lenY; iy++)
        {
          for (int iz = 0; iz < lenZ; iz++)
          {
            ftle[ix, iy, iz] = (ftle[ix, iy, iz] - min) / (max - min);
          }
        }
      }
    }

    private void GaussianFilter2D(ref float[,,] ftle)
    {
      if (lenZ > 1) return;

      float[,] filter = {
        { 1f / 256,  4f / 256,  6f / 256,  4f / 256, 1f / 256 },
        { 4f / 256, 16f / 256, 24f / 256, 16f / 256, 4f / 256 },
        { 6f / 256, 24f / 256, 36f / 256, 24f / 256, 6f / 256 },
        { 4f / 256, 16f / 256, 24f / 256, 16f / 256, 4f / 256 },
        { 1f / 256,  4f / 256,  6f / 256,  4f / 256, 1f / 256 },
      };

      float[,,] tmp = new float[lenX, lenY, 1];

      for (int ix = 0; ix < lenX; ix++)
      {
        for (int iy = 0; iy < lenY; iy++)
        {
          for (int fx = -2; fx < 3; fx++)
          {
            for (int fy = -2; fy < 3; fy++)
            {
              tmp[ix, iy, 0] += filter[fx + 2, fy + 2] * GetCoordValue(ftle, ix + fx, iy + fy, 1);
            }
          }
        }
      }
      ftle = tmp;
    }

    public void ShowForwardFTLE()
    {
      if (fFTLE == null)
      {
        return;
      }
      ShowFTLE(fFTLE);
    }

    public void ShowBackwardFTLE()
    {
      if (bFTLE == null)
      {
        return;
      }
      ShowFTLE(bFTLE);
    }

    private void ShowFTLE(float[,,] ftle)
    {
      for (int ix = 0; ix < lenX; ix++)
      {
        for (int iy = 0; iy < lenY; iy++)
        {
          for (int iz = 0; iz < lenZ; iz++)
          {
            Console.WriteLine($"{ix} {iy} {iz} : {ftle[ix, iy, iz]}");
          }
        }
      }
    }

    public void WriteForwardFTLE(string folderPath, string fileName)
    {
      if (fFTLE == null)
      {
        return;
      }
      string path = folderPath + '/' + fileName + "-f.txt";
      WriteFTLE(path, fFTLE);
    }

    public void WriteBackwardFTLE(string folderPath, string fileName)
    {
      if (bFTLE == null)
      {
        return;
      }
      string path = folderPath + '/' + fileName + "-b.txt";
      WriteFTLE(path, bFTLE);
    }

    private void WriteFTLE(string path, float[,,] ftle)
    {
      FileIO.WriteFTLEFile(path, ftle, lenX, lenY, lenZ);
    }

    public void WriteLCS(string folderPath, string fileName)
    {
      string path = folderPath + '/' + fileName + ".txt";
      WriteLCS(path);
    }

    private void WriteLCS(string path)
    {
      FileIO.WriteLCSFile(path, lcs, lenX, lenY, lenZ);
    }

    private float GetCoordValue(float[,,] ftle, int ix, int iy, int iz)
    {
      if (lenZ > 1)
      {
        if ((ix * (ix - lenX)) >= 0 || (iy * (iy - lenY)) >= 0 || (iz * (iz - lenZ)) >= 0) return 0;
        else return ftle[ix, iy, iz];
      }
      else
      {
        if ((ix * (ix - lenX)) >= 0 || (iy * (iy - lenY)) >= 0) return 0;
        else return ftle[ix, iy, 0];
      }

    }

    public void Dispose() { }
  }
}