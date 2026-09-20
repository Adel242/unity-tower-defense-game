using System.Collections.Generic;
using UnityEngine;

public abstract class TowerAttackData : ScriptableObject
{
    [SerializeField, Min(0.1f)] private float projectileSpeed = 10f;
    [SerializeField] private AudioClip shotSound;
    [SerializeField, Range(0f, 1f)] private float shotVolume = 0.5f;
    [SerializeField] private AudioClip impactSound;
    [SerializeField, Range(0f, 1f)] private float impactVolume = 0.35f;

    public float ProjectileSpeed => projectileSpeed;
    public AudioClip ShotSound => shotSound;
    public float ShotVolume => shotVolume;
    public AudioClip ImpactSound => impactSound;
    public float ImpactVolume => impactVolume;

    public abstract bool ApplyImpact(
        Vector3 attackOrigin,
        Vector3 impactPosition,
        EnemyHealth directTarget,
        float directDamage,
        HashSet<EnemyHealth> affectedEnemies,
        out Transform nextTarget
    );
}

public static class TowerAttackVfx
{
    private const string ParticleShader =
        "Universal Render Pipeline/Particles/Unlit";

    public static void PlayCannonExplosion(Vector3 position, float radius)
    {
        GameObject effectObject = new GameObject("Cannon Explosion VFX");
        effectObject.SetActive(false);
        effectObject.transform.position = position;

        ParticleSystem particles = effectObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.35f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(radius * 1.5f, radius * 2.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.32f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.25f, 0.02f, 1f),
            new Color(1f, 0.85f, 0.1f, 1f)
        );
        main.maxParticles = 12;
        main.stopAction = ParticleSystemStopAction.Destroy;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 10) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = Mathf.Max(0.1f, radius * 0.2f);

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime =
            particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(
            CreateFadeGradient(new Color(1f, 0.45f, 0.05f, 1f))
        );

        ParticleSystemRenderer renderer =
            effectObject.GetComponent<ParticleSystemRenderer>();
        Material effectMaterial = CreateMaterial(ParticleShader, Color.white);
        renderer.material = effectMaterial;
        Object.Destroy(effectMaterial, 1f);

        effectObject.SetActive(true);
        particles.Play();
    }

    public static void PlayLightningArc(Vector3 start, Vector3 end)
    {
        GameObject effectObject = new GameObject("Lightning Arc VFX");
        LightningArcEffect effect =
            effectObject.AddComponent<LightningArcEffect>();
        effect.Initialize(start, end, CreateMaterial(
            ParticleShader,
            new Color(0.25f, 0.75f, 1f, 1f)
        ));
    }

    public static void PlayFlameCone(
        Vector3 origin,
        Vector3 direction,
        float range,
        float angle
    )
    {
        GameObject effectObject = new GameObject("Flame Cone VFX");
        effectObject.SetActive(false);
        effectObject.transform.SetPositionAndRotation(
            origin,
            Quaternion.LookRotation(direction.normalized, Vector3.up)
        );

        ParticleSystem particles = effectObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.3f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(range * 1.8f, range * 2.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.65f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.12f, 0.01f, 1f),
            new Color(1f, 0.75f, 0.05f, 1f)
        );
        main.maxParticles = 48;
        main.stopAction = ParticleSystemStopAction.Destroy;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 40) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = angle * 0.5f;
        shape.radius = 0.15f;

        ParticleSystemRenderer renderer =
            effectObject.GetComponent<ParticleSystemRenderer>();
        Material effectMaterial = CreateMaterial(ParticleShader, Color.white);
        renderer.material = effectMaterial;
        Object.Destroy(effectMaterial, 1f);

        effectObject.SetActive(true);
        particles.Play();
    }

    private static Gradient CreateFadeGradient(Color color)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] {
                new GradientColorKey(color, 0f),
                new GradientColorKey(color, 1f)
            },
            new[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        return gradient;
    }

    private static Material CreateMaterial(string shaderName, Color color)
    {
        Shader shader = Shader.Find(shaderName);

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        Material material = new Material(shader);
        material.color = color;
        return material;
    }
}

internal sealed class LightningArcEffect : MonoBehaviour
{
    private const int PointCount = 9;
    private const float Duration = 0.15f;

    private LineRenderer lineRenderer;
    private Material material;
    private float remainingTime;

    public void Initialize(Vector3 start, Vector3 end, Material newMaterial)
    {
        material = newMaterial;
        remainingTime = Duration;

        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = PointCount;
        lineRenderer.startWidth = 0.12f;
        lineRenderer.endWidth = 0.035f;
        lineRenderer.numCapVertices = 2;
        lineRenderer.material = material;
        lineRenderer.startColor = new Color(0.7f, 0.95f, 1f, 1f);
        lineRenderer.endColor = new Color(0.1f, 0.45f, 1f, 1f);

        Vector3 direction = end - start;

        for (int i = 0; i < PointCount; i++)
        {
            float progress = i / (PointCount - 1f);
            Vector3 point = start + direction * progress;

            if (i > 0 && i < PointCount - 1)
            {
                point += Random.insideUnitSphere * 0.18f;
            }

            lineRenderer.SetPosition(i, point);
        }
    }

    private void Update()
    {
        remainingTime -= Time.deltaTime;
        float alpha = Mathf.Clamp01(remainingTime / Duration);

        Color startColor = lineRenderer.startColor;
        Color endColor = lineRenderer.endColor;
        startColor.a = alpha;
        endColor.a = alpha;
        lineRenderer.startColor = startColor;
        lineRenderer.endColor = endColor;

        if (remainingTime <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (material != null)
        {
            Destroy(material);
        }
    }
}

public static class TowerAttackAudio
{
    private const float MaximumShotDuration = 1.1f;
    private const float MaximumImpactDuration = 0.65f;
    private const float MaximumPlacementDuration = 0.65f;
    private const float ShotMixVolume = 0.5f;
    private const float ImpactMixVolume = 0.35f;
    private const int MaximumSimultaneousSounds = 8;
    private static readonly List<GameObject> ActiveSounds =
        new List<GameObject>();
    private static readonly Dictionary<int, float> LastPlayTimeByClip =
        new Dictionary<int, float>();

    public static void PlayShot(TowerAttackData attackData, Vector3 position)
    {
        AudioClip clip = attackData != null ? attackData.ShotSound : null;
        float volume = attackData != null ? attackData.ShotVolume : 0f;

        if (clip != null)
        {
            float cooldown = attackData is FlameAttackData ? 0.3f : 0.1f;
            PlayControlled(
                clip,
                position,
                volume * ShotMixVolume,
                MaximumShotDuration,
                cooldown,
                0.65f
            );
        }
    }

    public static void PlayImpact(TowerAttackData attackData, Vector3 position)
    {
        if (attackData != null && attackData.ImpactSound != null)
        {
            PlayControlled(
                attackData.ImpactSound,
                position,
                attackData.ImpactVolume * ImpactMixVolume,
                MaximumImpactDuration,
                0.08f,
                0.7f
            );
        }
    }

    public static void PlayPlacement(
        AudioClip clip,
        Vector3 position,
        float volume
    )
    {
        if (clip != null)
        {
            PlayControlled(
                clip,
                position,
                volume,
                MaximumPlacementDuration,
                0f,
                0f
            );
        }
    }

    public static void StopAll()
    {
        foreach (GameObject soundObject in ActiveSounds)
        {
            if (soundObject != null)
            {
                AudioSource source = soundObject.GetComponent<AudioSource>();
                source?.Stop();
                UnityEngine.Object.Destroy(soundObject);
            }
        }

        ActiveSounds.Clear();
        LastPlayTimeByClip.Clear();
    }

    private static void PlayControlled(
        AudioClip clip,
        Vector3 position,
        float volume,
        float maximumDuration,
        float minimumInterval,
        float spatialBlend
    )
    {
        ActiveSounds.RemoveAll(soundObject => soundObject == null);

        int clipId = clip.GetInstanceID();
        float currentTime = Time.unscaledTime;

        if (
            minimumInterval > 0f &&
            LastPlayTimeByClip.TryGetValue(clipId, out float lastPlayTime) &&
            currentTime - lastPlayTime < minimumInterval
        )
        {
            return;
        }

        LastPlayTimeByClip[clipId] = currentTime;

        if (ActiveSounds.Count >= MaximumSimultaneousSounds)
        {
            GameObject oldestSound = ActiveSounds[0];
            ActiveSounds.RemoveAt(0);

            if (oldestSound != null)
            {
                UnityEngine.Object.Destroy(oldestSound);
            }
        }

        GameObject soundObject = new GameObject($"SFX - {clip.name}");
        soundObject.transform.position = position;

        AudioSource source = soundObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.spatialBlend = spatialBlend;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 4f;
        source.maxDistance = 35f;
        source.Play();

        ActiveSounds.Add(soundObject);
        UnityEngine.Object.Destroy(
            soundObject,
            Mathf.Min(clip.length, maximumDuration)
        );
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        ActiveSounds.Clear();
        LastPlayTimeByClip.Clear();
    }
}
