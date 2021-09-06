using System;

namespace Arihara.GuideSmoke
{
  class LCS : IDisposable
  {
    private float[,,] fFTLE = null;
    private float[,,] bFTLE = null;
    private int[,,] lcs;
    private int lenX, lenY, lenZ;

    private bool isComputable;

    public bool IsComputable
    {
      get { return isComputable; }
    }

    public LCS(float[,,] forwardFTLE, float[,,] backwardFTLE, int lenX, int lenY, int lenZ)
    {
      if (forwardFTLE != null) fFTLE = forwardFTLE;
      if (backwardFTLE != null) bFTLE = backwardFTLE;
      this.lenX = lenX;
      this.lenY = lenY;
      this.lenZ = lenZ;
      lcs = new int[lenX, lenY, lenZ];

      if (fFTLE != null || bFTLE != null) isComputable = true;
      else isComputable = false;
    }

    public void ShowForwardFTLE() { ShowFTLE(fFTLE); }
    public void ShowBackwardFTLE() { ShowFTLE(bFTLE); }

    private void ShowFTLE(float[,,] ftle)
    {
      if (ftle == null)
      {
        Console.WriteLine("FTLE is no data.");
        return;
      }
      
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

    public void Dispose() { }
  }
}