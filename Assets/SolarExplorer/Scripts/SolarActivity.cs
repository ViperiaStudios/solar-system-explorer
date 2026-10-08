using UnityEngine;

namespace SolarExplorer
{
    public sealed class SolarActivity : MonoBehaviour
    {
        private sealed class Loop
        {
            public LineRenderer haze,core;
            public Vector3 normal,tangent,side;
            public float phase,span,height,period;
        }
        private readonly Loop[] loops=new Loop[12];
        private readonly Vector3[] points=new Vector3[48];

        public void Initialize(Material material)
        {
            var random=new System.Random(2608);
            for(int i=0;i<loops.Length;i++)
            {
                float a=i*Mathf.PI*2/loops.Length;
                var normal=Quaternion.Euler(60,0,0)*new Vector3(Mathf.Cos(a),Mathf.Sin(a),((float)random.NextDouble()-0.5f)*0.9f).normalized;
                var tangent=Vector3.Cross(normal,Vector3.up).normalized;
                loops[i]=new Loop {
                    normal=normal,tangent=tangent,side=Vector3.Cross(normal,tangent),
                    phase=(float)random.NextDouble(),span=0.09f+(float)random.NextDouble()*0.16f,
                    height=0.12f+(float)random.NextDouble()*0.22f,period=3.8f+(float)random.NextDouble()*2.6f,
                    haze=CreateLine("Prominence glow",material),core=CreateLine("Plasma stream",material)
                };
            }
            SetTime(0);
        }

        private LineRenderer CreateLine(string name,Material material)
        {
            var line=new GameObject(name,typeof(LineRenderer)).GetComponent<LineRenderer>();
            line.transform.SetParent(transform,false); line.useWorldSpace=false; line.positionCount=points.Length;
            line.sharedMaterial=material; line.textureMode=LineTextureMode.Stretch;
            line.widthCurve=new AnimationCurve(new Keyframe(0,0.12f),new Keyframe(0.25f,0.8f),new Keyframe(0.55f,1),new Keyframe(1,0.06f));
            line.numCapVertices=3;
            return line;
        }

        public void SetTime(float time)
        {
            transform.localRotation=Quaternion.Euler(0,time*3,0);
            foreach(var loop in loops)
            {
                float age=time/loop.period+loop.phase;
                int cycle=Mathf.FloorToInt(age);
                float life=age-cycle;
                // A short eruption rises, ripples, and disappears completely before
                // its next event. There is no permanently visible wire at the limb.
                float activity=Mathf.SmoothStep(0,1,life/0.14f)*(1-Mathf.SmoothStep(0,1,(life-0.48f)/0.36f));
                float rise=Mathf.SmoothStep(0,1,life/0.75f);
                Quaternion site=Quaternion.AngleAxis(cycle*137.5f,Vector3.up);
                Vector3 baseNormal=site*loop.normal, tangent=site*loop.tangent, side=site*loop.side;
                float radius=SolarLayout.SunRadius;
                for(int p=0;p<points.Length;p++)
                {
                    float t=(float)p/(points.Length-1),arc=Mathf.Sin(t*Mathf.PI);
                    float angle=(t-0.5f)*loop.span*(0.7f+rise*1.1f);
                    Vector3 normal=baseNormal*Mathf.Cos(angle)+tangent*Mathf.Sin(angle);
                    float ripple=Mathf.Sin(t*12-time*4.5f+loop.phase*10)*arc*radius*0.026f*activity;
                    points[p]=normal*radius*(0.994f+arc*loop.height*rise)+side*ripple;
                }
                loop.haze.SetPositions(points); loop.core.SetPositions(points);
                loop.haze.enabled=loop.core.enabled=activity>0.004f;
                loop.haze.widthMultiplier=radius*0.068f*activity;
                loop.core.widthMultiplier=radius*0.014f*activity;
                loop.haze.startColor=loop.haze.endColor=new Color(1,0.30f,0.035f,activity*0.24f);
                loop.core.startColor=loop.core.endColor=new Color(1,0.64f,0.18f,activity*0.68f);
            }
        }
    }
}
