using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.Tokenizers;

namespace JobMatchAI.Infrastructure.AI.Services;

public sealed class EmbeddingService : IEmbeddingService, IDisposable
{
    private readonly InferenceSession _session;
    private readonly BertTokenizer _tokenizer;
    private const int MaxSequenceLength = 256;

    public EmbeddingService(string modelPath, string vocabPath)
    {
        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException($"ONNX Model not found at:  {modelPath}");
        }

        _session = new InferenceSession(modelPath);
        _tokenizer = BertTokenizer.Create(vocabPath);
    }


    public float[] GenerateEmbeddings(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new float[384];
        }

        var encoded = _tokenizer.EncodeToIds(text);
        var inputIds = encoded.Take(MaxSequenceLength).Select(id => (long)id).ToArray();
        var attentionMask = Enumerable.Repeat(1L, inputIds.Length).ToArray();
        var tokenTypeIds = new long[inputIds.Length];

        var shape = new[] { 1, inputIds.Length };
        var inputIdsTensor = new DenseTensor<long>(inputIds, shape);
        var attentionMaskTensor = new DenseTensor<long>(attentionMask, shape);
        var tokenTypeIdsTensor = new DenseTensor<long>(tokenTypeIds, shape);

        var inputs = new NamedOnnxValue[]
        {
            NamedOnnxValue.CreateFromTensor("input_ids", inputIdsTensor),
            NamedOnnxValue.CreateFromTensor("attention_mask", attentionMaskTensor),
            NamedOnnxValue.CreateFromTensor("token_type_ids", tokenTypeIdsTensor)
        };

        using var results = _session.Run(inputs);
        var tensor = results.First(r => r.Value is DenseTensor<float>).AsTensor<float>();

        return MeanPooling(tensor, attentionMask);
    }

    public Task<float[]> GenerateEmbeddingsAsync(string text, CancellationToken ct = default)
    {
        return Task.Run(() => GenerateEmbeddings(text), ct);
    }

    private static float[] MeanPooling(Tensor<float> outputTensor, long[] attentionMask)
    {
        const int dimensions = 384;
        var embedding = new float[dimensions];
        var maskSum = attentionMask.Sum();

        if (maskSum == 0)
        {
            return embedding;
        }

        for (var i = 0; i < outputTensor.Dimensions[1]; i++)
        {
            var mask = attentionMask[i];
            if (mask != 1) continue;
            for (var j = 0; j < dimensions; j++)
            {
                embedding[i] += outputTensor[0, i, j];
            }
        }

        for (var j = 0; j < dimensions; j++)
        {
            embedding[j] /= maskSum;
        }

        return embedding;
    }

    public void Dispose()
    {
        _session.Dispose();
    }
}