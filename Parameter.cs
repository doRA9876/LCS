namespace Arihara.GuideSmoke
{
  public class Parameter
  {
    public string forwardFTLEPath { get; set; }
    public string backwardFTLEPath { get; set; }
    public string outLCSPath { get; set; }
    public int startT { get; set; }
    public int endT { get; set; }
    public int integralT { get; set; }
    public int ftleResolutionX { get; set; }
    public int ftleResolutionY { get; set; }
    public int ftleResolutionZ { get; set; }
    public float deltaX{ get; set; }
    public float deltaY{ get; set; }
    public float deltaZ{ get; set; }
    public int skeletonizeNum { get; set; }
    public int gaussianNum { get; set; }
    public string LcsMethodName { get; set; }
    public float lcsThreshold { get; set; }
    public float kappa { get; set; }
  }
}