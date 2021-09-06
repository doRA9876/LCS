using System;

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

      for (int t = 200; t < 900; t += 50)
      {
        string path = $"D:/Projects/MATLAB/FTLE/results/ftle-{t}.txt";
        float[,,] ftle = FileIO.ReadFTLEFile(path, 256, 256, 1);

        using (LCS lcs = new LCS(null, ftle, 256, 256, 1))
        {
          if(!lcs.IsComputable) continue;
        }
      }
    }
  }
}
