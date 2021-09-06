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
      string path = "D:/Projects/MATLAB/FTLE/results/ftle-200.txt";
      float[,,] ftle = FileIO.ReadFTLEFile(path, 256, 256, 1);

      for (int ix = 0; ix < 2; ix++)
      {
        for (int iy = 0; iy < 2; iy++)
        {
          Console.WriteLine($"{ftle[ix, iy, 0]}");
        }
      }
    }
  }
}
