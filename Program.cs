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
      Parameter parameter = ReadJson("./parameter.json");

      for (int t = parameter.startT; t < parameter.endT; t += parameter.integralT)
      {
        float[,,] fFTLE = null, bFTLE = null;
        int lenX = parameter.ftleResolutionX;
        int lenY = parameter.ftleResolutionY;
        int lenZ = parameter.ftleResolutionZ;
        if (!string.IsNullOrEmpty(parameter.forwardFTLEPath))
        {
          string fFTLEPath = parameter.forwardFTLEPath + '/' + $"ftle-{t}.txt";
          fFTLE = FileIO.ReadFTLEFile(fFTLEPath, lenX, lenY, lenZ);
        }

        if (!string.IsNullOrEmpty(parameter.backwardFTLEPath))
        {
          string bFTLEPath = parameter.backwardFTLEPath + '/' + $"ftle-{t}.txt";
          bFTLE = FileIO.ReadFTLEFile(bFTLEPath, lenX, lenY, lenZ);
        }

        using (LCS lcs = new LCS(fFTLE, bFTLE, lenX, lenY, lenZ))
        {
          if (!lcs.IsComputable) continue;
          lcs.ShowForwardFTLE();
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
