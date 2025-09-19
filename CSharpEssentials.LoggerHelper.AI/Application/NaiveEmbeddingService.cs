using CSharpEssentials.LoggerHelper.AI.Domain;

namespace CSharpEssentials.LoggerHelper.AI.Application;

public sealed class NaiveEmbeddingService : IEmbeddingService {
    const int D = 16;

    public Task<float[]> EmbedAsync(string text) {
        var v = new float[D];
        int i = 0;
        foreach (var ch in text ?? string.Empty)
            v[i++ % D] += ch;
        var norm = Math.Sqrt(v.Sum(x => x * x));
        if (norm > 0)
            for (int k = 0; k < D; k++)
                v[k] = (float)(v[k] / norm);
        return Task.FromResult(v);
    }
    public double Cosine(float[] a, float[] b) => a.Zip(b, (x, y) => x * y).Sum();
}