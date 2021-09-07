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
        float[,,] fFTLE = null, bFTLE = null;
        int lenX = p.ftleResolutionX;
        int lenY = p.ftleResolutionY;
        int lenZ = p.ftleResolutionZ;
        if (!string.IsNullOrEmpty(p.forwardFTLEPath))
        {
          string fFTLEPath = p.forwardFTLEPath + '/' + $"ftle-{t}.txt";
          fFTLE = FileIO.ReadFTLEFile(fFTLEPath, lenX, lenY, lenZ);
        }

        if (!string.IsNullOrEmpty(p.backwardFTLEPath))
        {
          string bFTLEPath = p.backwardFTLEPath + '/' + $"ftle-{t}.txt";
          bFTLE = FileIO.ReadFTLEFile(bFTLEPath, lenX, lenY, lenZ);
        }

        using (LCS lcs = new LCS(fFTLE, bFTLE, lenX, lenY, lenZ))
        {
          if (!lcs.IsComputable) continue;
          lcs.Calculation(p.LcsMethodName, p.gaussianNum, p.kappa, p.lcsThreshold, p.skeletonizeNum);
          lcs.WriteForwardFTLE("./data/FTLE/results", $"ftle-{t}");
          lcs.WriteBackwardFTLE("./data/FTLE/results", $"ftle-{t}");
          lcs.WriteLCS("./data/LCS/results", $"lcs-{t}");
        }
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
