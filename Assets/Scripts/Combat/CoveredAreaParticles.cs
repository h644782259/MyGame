using UnityEngine;
namespace Emberfall
{
    // Cosmetic clipping only; damage continues to use CombatSight.Area at the hit.
    internal sealed class CoveredAreaParticles : MonoBehaviour
    {
        private ParticleSystem system;
        private readonly ParticleSystem.Particle[] samples=new ParticleSystem.Particle[90];
        private void Awake(){system=GetComponent<ParticleSystem>();}
        private void LateUpdate()
        {
            if(system==null)return;int n=system.GetParticles(samples);
            for(int i=0;i<n;i++)if(!CombatSight.VisualFootprint(transform.position,samples[i].position,.2f))samples[i].remainingLifetime=0;
            system.SetParticles(samples,n);
        }
    }
}
