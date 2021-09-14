using System;
using System.IO;
using System.Numerics;
using System.Text;
using System.Text.Json;

namespace Arihara.GuideSmoke
{
  class Program
  {
    static void Main(string[] args)
    {
      Sample();
    }

    static void Sample()
    {
      Parameter p = ReadJson("./parameter.json");

      for (int t = p.startT; t <= p.endT; t += p.integralT)
      {
        Console.WriteLine($"Start LCS Calculation : t = {t}");

        float[,,] fFTLE = null, bFTLE = null;
        bool isLoadingSeccessful = false;
        int lenX = p.ftleResolutionX;
        int lenY = p.ftleResolutionY;
        int lenZ = p.ftleResolutionZ;
        if (!string.IsNullOrEmpty(p.forwardFTLEPath))
        {
          string fFTLEPath = p.forwardFTLEPath + '/' + $"ftle-{t}.txt";
          if (FileIO.LoadFTLEFile(fFTLEPath, ref fFTLE, lenX, lenY, lenZ))
            isLoadingSeccessful = true;
        }

        if (!string.IsNullOrEmpty(p.backwardFTLEPath))
        {
          string bFTLEPath = p.backwardFTLEPath + '/' + $"ftle-{t}.txt";
          if (FileIO.LoadFTLEFile(bFTLEPath, ref bFTLE, lenX, lenY, lenZ))
            isLoadingSeccessful = true;
        }

        if (!isLoadingSeccessful) continue;
        bool[,,] fRegion3D = null, bRegion3D = null;
        using (LCS lcs = new LCS(fFTLE, bFTLE, lenX, lenY, lenZ, p.deltaX, p.deltaY, p.deltaZ))
        {
          if (!lcs.IsComputable) continue;
          lcs.SetParameters(p.kappa, p.lcsThreshold);
          lcs.Calculation(p.LcsMethodName, p.gaussianNum, p.skeletonizeNum);
          lcs.WriteLCS(p.outLCSPath, $"lcs-{t}");
          fRegion3D = lcs.ForwardRegion;
          bRegion3D = lcs.BackwardRegion;
        }

        Vector2[,] position = new Vector2[lenX, lenY];
        for (int ix = 0; ix < lenX; ix++)
        {
          for (int iy = 0; iy < lenY; iy++)
          {
            position[ix, iy] = new Vector2(ix, iy);
          }
        }

        if (fRegion3D != null && fFTLE != null)
        {
          bool[,] fRegion2D = Array3DTo2D<bool>(fRegion3D, 0);
          float[,] fFTLE2D = Array3DTo2D<float>(fFTLE, 0);
          using (RidgeRefine2D ridgeRefine = new RidgeRefine2D(fRegion2D, fFTLE2D, position, lenX, lenY, p.deltaX, p.deltaY))
          {
            Console.Write("Start Forward FTLE Ridge Refine");
          }
        }

        if (bRegion3D != null && bFTLE != null)
        {
          bool[,] bRegion2D = Array3DTo2D<bool>(bRegion3D, 0);
          float[,] bFTLE2D = Array3DTo2D<float>(bFTLE, 0);
          using (RidgeRefine2D ridgeRefine = new RidgeRefine2D(bRegion2D, bFTLE2D, position, lenX, lenY, p.deltaX, p.deltaY))
          {
            Console.Write("Start Backward FTLE Ridge Refine");
            ridgeRefine.SetParameters(p.refinementIteration, p.delta, p.simga, p.omega, p.d_max, p.k_cut);
            ridgeRefine.SubPixelRidgeRefinement();
            // ridgeRefine.ShowPixelInfo();
            int[,] result = ridgeRefine.GetResults();
            string path = p.outLCSPath + '/' + $"lcs-{t}.txt";
            // FileIO.WriteLCSFile(path, Array2DTo3D(result, 1), lenX, lenY, lenZ);
            // FileIO.WriteGradientFile($"./data/gradients-{t}.txt", ridgeRefine.Gradients, lenX, lenY);
          }
        }

        Console.WriteLine($"End LCS Calculation");
      }
    }

    static Parameter ReadJson(string jsonPath)
    {
      string jsonStr;
      using (StreamReader sr = new StreamReader(jsonPath, Encoding.GetEncoding("utf-8")))
      {
        jsonStr = sr.ReadToEnd();
      }

      Console.WriteLine(jsonStr);

      Parameter parameter = new Parameter();
      parameter = JsonSerializer.Deserialize<Parameter>(jsonStr);

      return parameter;
    }

    static T[,] Array3DTo2D<T>(T[,,] input, int z)
    {
      int lx = input.GetLength(0);
      int ly = input.GetLength(1);
      T[,] output = new T[lx, ly];
      for (int ix = 0; ix < lx; ix++)
      {
        for (int iy = 0; iy < ly; iy++)
        {
          output[ix, iy] = input[ix, iy, z];
        }
      }
      return output;
    }

    static T[,,] Array2DTo3D<T>(T[,] input, int lenZ)
    {
      int lx = input.GetLength(0);
      int ly = input.GetLength(1);
      T[,,] output = new T[lx, ly, lenZ];
      for (int ix = 0; ix < lx; ix++)
      {
        for (int iy = 0; iy < ly; iy++)
        {
          for (int iz = 0; iz < lenZ; iz++)
          {
            output[ix, iy, iz] = input[ix, iy];
          }
        }
      }
      return output;
    }
  }
}
