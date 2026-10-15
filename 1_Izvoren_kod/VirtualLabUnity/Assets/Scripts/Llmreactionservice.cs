using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

// What the LLM must return. Field names must match the JSON keys exactly.
[Serializable]
public class ReactionResult
{
    public string description;        // short Macedonian text (Latin letters)
    public float[] liquid_color;      // [r,g,b,a] each 0..1
    public bool gas;                  // bubbles + smoke
    public bool precipitate;          // solid layer at the bottom
    public float[] precipitate_color; // [r,g,b,a]
    public float temperature_change;  // degrees C, -20..+40
    public string sound;              // "chime", "fizz", "low", "none"
}

// ---- Gemini request/response shapes ----
[Serializable] class GPart { public string text; }
[Serializable] class GSystemInstruction { public GPart[] parts; }
[Serializable] class GUserContent { public string role = "user"; public GPart[] parts; }
[Serializable] class GGenerationConfig { public float temperature = 0f; public string responseMimeType = "application/json"; }
[Serializable] class GRequest
{
    public GSystemInstruction systemInstruction;
    public GUserContent[] contents;
    public GGenerationConfig generationConfig = new GGenerationConfig();
}
[Serializable] class GRespContent { public GPart[] parts; }
[Serializable] class GCandidate { public GRespContent content; }
[Serializable] class GResponse { public GCandidate[] candidates; }

public class LlmReactionService : MonoBehaviour
{
    // Gemini key from Google AI Studio (aistudio.google.com). Do not commit it to Git.
    public string ApiKey = "";
    // If you get a 404, check ai.google.dev/gemini-api/docs/models for a current model name.
    // Lighter alternative if the free quota is a problem: "gemini-3.5-flash-lite"
    public string Model = "gemini-3.8-flash";
    // Used automatically when the main model is overloaded (503) or rate limited (429)
    public string FallbackModel = "gemini-3.5-flash-lite";

    // Short description of the last failure (shown on the lab panel)
    public string LastError = "";

    private readonly Dictionary<string, ReactionResult> cache = new Dictionary<string, ReactionResult>();

    private const string SystemPrompt =
        "You are the chemistry engine of a school virtual lab. The user lists substances that were mixed in a beaker. " +
        "Decide the most realistic visible result at room temperature. " +
        "Respond ONLY with a JSON object with exactly these keys: " +
        "description (string, max 25 words, Macedonian written in Latin letters without diacritics), " +
        "liquid_color (array of 4 numbers 0..1 = r,g,b,a), " +
        "gas (boolean, true if gas bubbles are released), " +
        "precipitate (boolean, true if a solid forms), " +
        "precipitate_color (array of 4 numbers 0..1), " +
        "temperature_change (number in degrees C between -20 and 40), " +
        "sound (one of: chime, fizz, low, none). " +
        "If nothing visibly happens, set gas and precipitate to false, keep a plausible liquid_color and say so in description. " +
        "Never invent dangerous or non-realistic effects.";

    public IEnumerator GetReaction(List<string> substanceDescriptions, Action<ReactionResult> onDone)
    {
        var sorted = new List<string>(substanceDescriptions);
        sorted.Sort();
        string key = string.Join("|", sorted);

        if (cache.TryGetValue(key, out var cached))
        {
            onDone(cached);
            yield break;
        }

        var request = new GRequest
        {
            systemInstruction = new GSystemInstruction { parts = new[] { new GPart { text = SystemPrompt } } },
            contents = new[]
            {
                new GUserContent { parts = new[] { new GPart { text = "Mixed substances: " + string.Join(", ", sorted) } } }
            }
        };

        byte[] body = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(request));

        // Attempt 1: main model, attempt 2: fallback model, attempt 3: main model again after a pause
        string[] models = { Model, FallbackModel, Model };
        for (int attempt = 0; attempt < models.Length; attempt++)
        {
            if (attempt == 2) yield return new WaitForSeconds(2f);

            string url = "https://generativelanguage.googleapis.com/v1beta/models/" + models[attempt] + ":generateContent";
            using (var www = new UnityWebRequest(url, "POST"))
            {
                www.uploadHandler = new UploadHandlerRaw(body);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");
                www.SetRequestHeader("x-goog-api-key", ApiKey);
                www.timeout = 30;

                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning("LLM request failed (" + models[attempt] + "): " + www.error + "\n" + www.downloadHandler.text);
                    LastError = "HTTP " + www.responseCode + " " + ShortMessage(www.downloadHandler.text, www.error);

                    // Temporary problems: try the next model. Anything else (bad key, bad request) will not improve.
                    long code = www.responseCode;
                    bool retryable = code == 503 || code == 429 || code == 500 || code == 404 || code == 0;
                    if (retryable) continue;

                    onDone(null);
                    yield break;
                }

                ReactionResult result = null;
                try
                {
                    var response = JsonUtility.FromJson<GResponse>(www.downloadHandler.text);
                    string json = StripFences(response.candidates[0].content.parts[0].text);
                    result = JsonUtility.FromJson<ReactionResult>(json);
                    Sanitize(result);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("LLM response could not be parsed: " + e.Message + "\n" + www.downloadHandler.text);
                    LastError = "odgovorot ne moze da se procita (" + e.Message + ")";
                }

                if (result != null)
                {
                    LastError = "";
                    cache[key] = result;
                }
                onDone(result);
                yield break;
            }
        }

        // all attempts failed; LastError holds the last reason
        onDone(null);
    }

    // Pulls the "message" field out of Google's error JSON (or falls back to the Unity error)
    private static string ShortMessage(string body, string fallback)
    {
        if (!string.IsNullOrEmpty(body))
        {
            int i = body.IndexOf("\"message\"");
            if (i >= 0)
            {
                int q1 = body.IndexOf('"', body.IndexOf(':', i) + 1);
                int q2 = q1 >= 0 ? body.IndexOf('"', q1 + 1) : -1;
                if (q1 >= 0 && q2 > q1) return Truncate(body.Substring(q1 + 1, q2 - q1 - 1), 110);
            }
        }
        return Truncate(fallback, 110);
    }

    private static string Truncate(string t, int max)
    {
        if (string.IsNullOrEmpty(t)) return "";
        return t.Length <= max ? t : t.Substring(0, max);
    }

    // In case the model wraps the JSON in ```json ... ```
    private static string StripFences(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        text = text.Trim();
        int start = text.IndexOf('{');
        int end = text.LastIndexOf('}');
        if (start >= 0 && end > start) return text.Substring(start, end - start + 1);
        return text;
    }

    // Never trust model output: clamp everything before it touches the scene.
    private static void Sanitize(ReactionResult r)
    {
        if (r == null) return;
        if (string.IsNullOrEmpty(r.description)) r.description = "Nema vidliva reakcija.";
        if (r.description.Length > 160) r.description = r.description.Substring(0, 160);
        r.liquid_color = FixColor(r.liquid_color, new float[] { .25f, .68f, .93f, .78f });
        r.precipitate_color = FixColor(r.precipitate_color, new float[] { .3f, .3f, .3f, 1f });
        r.temperature_change = Mathf.Clamp(r.temperature_change, -20f, 40f);
        if (r.sound != "chime" && r.sound != "fizz" && r.sound != "low") r.sound = "none";
    }

    private static float[] FixColor(float[] c, float[] fallback)
    {
        if (c == null || c.Length < 3) return fallback;
        return new float[]
        {
            Mathf.Clamp01(c[0]), Mathf.Clamp01(c[1]), Mathf.Clamp01(c[2]),
            c.Length > 3 ? Mathf.Clamp(c[3], .4f, 1f) : .8f
        };
    }
}