using Unity;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.Rendering;

public struct HealthBar : IComponentData
{

}

public class HealthBarRendererSystem : ComponentSystem
{
    Texture2D healthBarTexture;
    Material healthBarMaterial;

    Texture2D healthBarBackgroundTexture;
    Material healthBarBackgroundMaterial;


    ComponentGroup m_InstanceRendererGroup;
    protected override void OnCreateManager()
    {
        healthBarTexture = Resources.Load<Texture2D>("healthbarTexture");
        healthBarMaterial = Resources.Load<Material>("healthbarMaterial");
        healthBarBackgroundTexture = Resources.Load<Texture2D>("backgroundTexture");
        healthBarBackgroundMaterial = Resources.Load<Material>("backgroundMaterial");
        m_InstanceRendererGroup = GetComponentGroup(ComponentType.Create<HealthBar>(), ComponentType.Create<Position>(), ComponentType.Create<Health>());
    }
    protected override void OnUpdate()
    {

        Camera.onPostRender = null;
        Camera.onPostRender += (Camera camera) =>
        {
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, Screen.width, 0, Screen.height);
        };

        var positions = m_InstanceRendererGroup.GetComponentDataArray<Position>();
        var health = m_InstanceRendererGroup.GetComponentDataArray<Health>();

        for(int i = 0; i != positions.Length; i++)
        {
            float3 position = positions[i].Value;
            Health h = health[i];
            Camera.onPostRender += (Camera camera) =>
            {
                float3 pos = Camera.current.WorldToScreenPoint(position);

                if(math.distance((float3)Camera.current.transform.position, position) < 25.0f)
                {
                    var width = 50;
                    var healthbarWidth = ((float)h.currentHealth / (float)h.maxHP) * (float)width;
                    var height = 10;
                    var offset = new float2(-25, 50);

                    Graphics.DrawTexture(
                    new Rect(pos.x + offset.x, pos.y + offset.y, width, height),
                    healthBarBackgroundTexture);

                    Graphics.DrawTexture(
                    new Rect(pos.x + offset.x, pos.y + offset.y, healthbarWidth, height),
                    healthBarTexture);
                }
            };

        }
        Camera.onPostRender += (Camera camera) =>
        {
            GL.PopMatrix();
        };
    }
}