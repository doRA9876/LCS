using System;
using System.Numerics;

namespace Arihara.GuideSmoke
{
  static class Eigen
  {
    static public float GetMaxEigenValue2x2(float[,] tensor)
    {
      float b = tensor[0, 0] + tensor[1, 1];
      float c = tensor[0, 0] * tensor[1, 1] - tensor[1, 0] * tensor[0, 1];
      float d = b * b - 4 * c;

      if (d < 0) return 0;
      d = (float)Math.Sqrt(d);

      float e1 = 0.5f * (b + d);
      float e2 = 0.5f * (b - d);

      if (e1 < e2) return e2;
      else return e1;
    }

    /*
    refer:http://nkl.cc.u-tokyo.ac.jp/13n/Eigen.pdf
    Power Method
    */
    static public float GetMaxEigenValue3x3(float[,] tensor)
    {
      Vector3 x = new Vector3(1, 0, 0);
      Vector3 y = new Vector3(1, 0, 0);
      int stepNum = 10;
      for (int k = 0; k < stepNum; k++)
      {
        x = Vector3.Normalize(y);
        float a0 = tensor[0, 0] * x.X + tensor[0, 1] * x.Y + tensor[0, 2] * x.Z;
        float a1 = tensor[1, 0] * x.X + tensor[1, 1] * x.Y + tensor[1, 2] * x.Z;
        float a2 = tensor[2, 0] * x.X + tensor[2, 1] * x.Y + tensor[2, 2] * x.Z;
        y = new Vector3(a0, a1, a2);
      }
      return Vector3.Dot(x, y);
    }
  }
}