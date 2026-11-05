using UnityEngine;

// The time of day: a run leaves in the morning and every leg to the next station takes three hours, so eight stations
// are a whole day, through the evening, the night and the small hours. One directional light is the sun by day and
// the moon by night, going round the sky without a break, so nothing in the picture ever jumps. Nearly all the
// picture's light comes from it (see the shadow colour in Resources/Toon.mat), so it alone sets the mood.
public partial class TrainSim
{
    const float FirstHour = 8f, HoursPerLeg = 3f;
    const float MoonLux = 42000f, NoonLux = 110000f;
    const float FastestHours = 0.35f; // hours a second the light may run: a skipped ride catches up, it does not jump

    public float Hour => Mathf.Repeat(FirstHour + HoursPerLeg * (Stations + (AtStation ? 0f : Mathf.Clamp01(1f - remaining / LegLength))), 24f);
    float shownHour = -1f;

    // In order through the day, the last one meeting the first round midnight: the hour, its colour, its lux.
    static readonly (float hour, Color colour, float lux)[] Daylight =
    {
        (0f, new Color(0.58f, 0.68f, 1f), MoonLux),
        (4f, new Color(0.52f, 0.6f, 0.98f), 36000f),     // the small hours: the deepest blue
        (5.5f, new Color(0.72f, 0.62f, 0.85f), 40000f),
        (6.5f, new Color(1f, 0.66f, 0.5f), 52000f),      // sunrise
        (8f, new Color(1f, 0.84f, 0.68f), 82000f),
        (10f, new Color(1f, 0.93f, 0.82f), 102000f),
        (12f, new Color(1f, 0.96f, 0.88f), NoonLux),
        (15f, new Color(1f, 0.92f, 0.78f), 102000f),
        (16.5f, new Color(1f, 0.78f, 0.56f), 90000f),
        (17.5f, new Color(1f, 0.62f, 0.42f), 70000f),    // the golden hour
        (18.5f, new Color(0.86f, 0.55f, 0.58f), 50000f), // dusk
        (19.5f, new Color(0.66f, 0.6f, 0.86f), MoonLux),
        (21f, new Color(0.58f, 0.68f, 1f), MoonLux),
    };

    void TurnSun()
    {
        if (!sun) return;
        float wanted = Hour, most = FastestHours * Time.unscaledDeltaTime;
        shownHour = shownHour < 0f ? wanted : Mathf.Repeat(shownHour + Mathf.Clamp(Mathf.DeltaAngle(shownHour * 15f, wanted * 15f) / 15f, -most, most), 24f);
        float hour = shownHour;
        int next = 1;
        while (next < Daylight.Length && Daylight[next].hour <= hour) next++;
        var from = Daylight[next - 1];
        var to = next < Daylight.Length ? Daylight[next] : (hour: 24f, Daylight[0].colour, Daylight[0].lux);
        float t = Mathf.InverseLerp(from.hour, to.hour, hour);
        Light light = sun.GetComponent<Light>();
        light.color = Color.Lerp(from.colour, to.colour, t);
        float lux = Mathf.Lerp(from.lux, to.lux, t);
        // The darkest grounds would go black by moonlight: a stronger moon there, easing into the same noon.
        float moon = BiomeId switch { "jungle" => 2.4f, "volcano" => 1.7f, _ => 1f };
        light.intensity = lux < MoonLux ? lux * moon : Mathf.Lerp(MoonLux * moon, NoonLux, Mathf.InverseLerp(MoonLux, NoonLux, lux));

        // Once round the sky in a day, rising where the other set: low at dawn and dusk (never on the horizon, where a
        // shadow would reach across the whole picture), highest at noon and midnight.
        float arc = Mathf.Repeat(hour - 6f, 24f) / 24f, up = Mathf.Sin(arc * Mathf.PI * 2f);
        light.transform.rotation = Quaternion.Euler(Mathf.Lerp(15f, up > 0f ? 58f : 52f, up * up), 105f + 360f * arc, 0f);
    }
}
