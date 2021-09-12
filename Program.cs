using System;
using System.IO;
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

      for (int t = p.startT; t < p.endT; t += p.integralT)
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
          if(FileIO.LoadFTLEFile(fFTLEPath, ref fFTLE, lenX, lenY, lenZ)) 
            isLoadingSeccessful = true;
        }

        if (!string.IsNullOrEmpty(p.backwardFTLEPath))
        {
          string bFTLEPath = p.backwardFTLEPath + '/' + $"ftle-{t}.txt";
          if(FileIO.LoadFTLEFile(bFTLEPath, ref bFTLE, lenX, lenY, lenZ))
            isLoadingSeccessful = true;
        }

        if(!isLoadingSeccessful) continue;

        using (LCS lcs = new LCS(fFTLE, bFTLE, lenX, lenY, lenZ, p.deltaX, p.deltaY, p.deltaZ))
        {
          if (!lcs.IsComputable) continue;
          lcs.SetParameters(p.kappa, p.lcsThreshold);
          lcs.Calculation(p.LcsMethodName, p.gaussianNum, p.skeletonizeNum);
          lcs.WriteLCS(p.outLCSPath, $"lcs-{t}");
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
  }
}
