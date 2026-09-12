using NAudio.Wave;
using NLayer.NAudioSupport;
using Silk.NET.OpenAL;

namespace LearnSilkNET.src;

public class ISoundEngine
{
    private readonly ALContext _alc = ALContext.GetApi();
    private readonly AL _al = AL.GetApi();
    private unsafe Device* _device;
    private unsafe Context* _context;
    private bool _initialized;
    private readonly List<uint> _sources = new();
    private readonly List<uint> _buffers = new();

    public ISoundEngine()
    {
        unsafe
        {
            _device = _alc.OpenDevice("");
            if (_device == null) { Console.Error.WriteLine("Áudio desativado: dispositivo OpenAL indisponível."); return; }
            _context = _alc.CreateContext(_device, null);
            if (_context == null || !_alc.MakeContextCurrent(_context))
            {
                Console.Error.WriteLine("Áudio desativado: contexto OpenAL indisponível.");
                if (_context != null) _alc.DestroyContext(_context);
                _alc.CloseDevice(_device); _context = null; _device = null; return;
            }
        }
        _al.GetError();
        _initialized = true;
    }

    public void Play2D(string filePath, bool shouldLoop)
    {
        if (!_initialized) return;
        uint source = 0, buffer = 0;
        try
        {
            var audio = LoadPcm16(filePath);
            BufferFormat format = audio.Channels switch
            {
                1 => BufferFormat.Mono16,
                2 => BufferFormat.Stereo16,
                _ => throw new NotSupportedException($"OpenAL suporta apenas mono/estéreo: {audio.Channels} canais.")
            };
            source = _al.GenSource(); buffer = _al.GenBuffer();
            if (source == 0 || buffer == 0) throw new InvalidOperationException("OpenAL não criou source/buffer.");
            _al.SetSourceProperty(source, SourceBoolean.Looping, shouldLoop);
            unsafe { fixed (byte* data = audio.Data) _al.BufferData(buffer, format, data, audio.Data.Length, audio.SampleRate); }
            CheckAl($"carregar '{filePath}'");
            _al.SetSourceProperty(source, SourceInteger.Buffer, buffer); _al.SourcePlay(source);
            CheckAl($"reproduzir '{filePath}'");
            _sources.Add(source); _buffers.Add(buffer);
            Console.WriteLine($"Áudio carregado: {audio.Channels} canais, {audio.SampleRate} Hz, {audio.Data.Length} bytes");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Não foi possível reproduzir '{filePath}': {ex.Message}");
            if (source != 0) { _al.SourceStop(source); _al.DeleteSource(source); }
            if (buffer != 0) _al.DeleteBuffer(buffer);
        }
    }

    private static (byte[] Data, int SampleRate, int Channels) LoadPcm16(string path)
    {
        using WaveStream reader = Path.GetExtension(path).ToLowerInvariant() switch
        {
            // Mp3WaveFormat é o formato comprimido de entrada; WaveFormat é a saída PCM do NLayer.
            ".mp3" => new Mp3FileReaderBase(path, wf => new Mp3FrameDecompressor(wf)),
            ".wav" => new WaveFileReader(path),
            _ => throw new NotSupportedException($"Formato não suportado: {Path.GetExtension(path)}")
        };
        using var stream = new MemoryStream(); reader.CopyTo(stream);
        return ConvertToPcm16(stream.ToArray(), reader.WaveFormat, path);
    }

    private static (byte[] Data, int SampleRate, int Channels) ConvertToPcm16(byte[] input, WaveFormat f, string path)
    {
        if (f.Channels < 1 || f.SampleRate < 1) throw new InvalidDataException($"Metadados inválidos: '{path}'.");
        if (f.Encoding == WaveFormatEncoding.Pcm && f.BitsPerSample == 16) return (input, f.SampleRate, f.Channels);
        int size = f.BitsPerSample / 8;
        if (size is < 1 or > 4 || input.Length % size != 0) throw new NotSupportedException($"Formato não suportado em '{path}': {f}.");
        byte[] output = new byte[input.Length / size * 2];
        for (int i = 0; i < input.Length / size; i++)
        {
            int o = i * size;
            float sample = f.Encoding == WaveFormatEncoding.IeeeFloat && f.BitsPerSample == 32
                ? BitConverter.ToSingle(input, o)
                : f.BitsPerSample switch
                {
                    8 => (input[o] - 128) / 128f,
                    16 => BitConverter.ToInt16(input, o) / 32768f,
                    24 => (((input[o] | input[o + 1] << 8 | input[o + 2] << 16) << 8) >> 8) / 8388608f,
                    32 => BitConverter.ToInt32(input, o) / 2147483648f,
                    _ => throw new NotSupportedException($"PCM de {f.BitsPerSample} bits não suportado.")
                };
            short value = (short)(Math.Clamp(sample, -1f, 1f) * 32767f);
            output[i * 2] = (byte)value; output[i * 2 + 1] = (byte)(value >> 8);
        }
        return (output, f.SampleRate, f.Channels);
    }

    private void CheckAl(string operation)
    {
        AudioError error = _al.GetError();
        if (error != AudioError.NoError) throw new InvalidOperationException($"Erro OpenAL ao {operation}: {error}");
    }

    public void Drop()
    {
        if (_initialized)
        {
            foreach (uint source in _sources) { _al.SourceStop(source); _al.DeleteSource(source); }
            foreach (uint buffer in _buffers) _al.DeleteBuffer(buffer);
            unsafe { _alc.DestroyContext(_context); _alc.CloseDevice(_device); }
        }
        _al.Dispose(); _alc.Dispose(); _initialized = false;
    }
}
