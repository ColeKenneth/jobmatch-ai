using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.Tokenizers;

namespace JobMatchAI.Infrastructure.AI.Services;

public sealed class EmbeddingService : IEmbeddingService, IDisposable
{
    private readonly InferenceSession _session;
    private readonly BertTokenizer _tokenizer;
    private const int MaxSequenceLength = 256;
    private const int EmbeddingDimensions = 384;

    public EmbeddingService(string modelPath, string vocabPath)
    {
        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException($"ONNX Model not found at:  {modelPath}");
        }

        if (!File.Exists(vocabPath))
        {
            throw new FileNotFoundException($"Vocabulary file not found at: {vocabPath}");
        }
        _session = new InferenceSession(modelPath);
        _tokenizer = BertTokenizer.Create(vocabPath);
    }


    public float[] GenerateEmbeddings(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new float[EmbeddingDimensions];
        }

        var encoded = _tokenizer.EncodeToIds(text);
        var inputIds = encoded.Take(MaxSequenceLength).Select(id => (long)id).ToArray();

        if (inputIds.Length == 0)
        {
            return new float[EmbeddingDimensions];
        }
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

        var pooled = MeanPooling(tensor, attentionMask);
        return NormalizeL2(pooled);
    }

    public Task<float[]> GenerateEmbeddingsAsync(string text, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();
            return GenerateEmbeddings(text);
        }, ct);
    }

    private static float[] MeanPooling(Tensor<float> outputTensor, long[] attentionMask)
    {
        var embedding = new float[EmbeddingDimensions];
        var seqLength = Math.Min(outputTensor.Dimensions[1], attentionMask.Length);
        var dims = Math.Min(outputTensor.Dimensions[2], EmbeddingDimensions);

        long maskSum = 0;

        for (var i = 0; i < seqLength; i++)
        {
            if (attentionMask[i] != 1)
            {
                continue;
            }

            maskSum++;
            for (var j = 0; j < dims; j++)
            {
                embedding[j] += outputTensor[0, i, j];
            }
        }

        if (maskSum == 0)
        {
            return embedding;
        }

        for (var j = 0; j < dims; j++)
        {
            embedding[j] /= maskSum;
        }

        return embedding;
    }

    private static float[] NormalizeL2(float[] vector)
    {
        var sumSquares = vector.Sum(t => (double)t * t);

        var norm = Math.Sqrt(sumSquares);
        if (norm <= 1e-12)
        {
            return vector;
        }
        
        var normalized = new float[vector.Length];
        for (var i = 0; i < vector.Length; i++)
        {
            normalized[i] = (float)(vector[i] / norm);
        }

        return normalized;
    }

    public void Dispose()
    {
        _session.Dispose();
    }
}