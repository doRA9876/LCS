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

    public void Dispose() { }
  }
}