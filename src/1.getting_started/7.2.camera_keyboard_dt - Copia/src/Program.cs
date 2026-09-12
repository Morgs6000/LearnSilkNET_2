using System.Numerics;
using Silk.NET.GLFW;
using Silk.NET.OpenGL;
using StbImageSharp;

namespace LearnSilkNET.src;

public class Program
{
    private static Glfw _glfw = null!;
    private static GL _gl = null!;

    // Ancoragem estática explícita para evitar coleta do Garbage Collector
    private static GlfwCallbacks.WindowRefreshCallback _refreshCallback = null!;

    // Configurações
    private static uint SCR_WIDTH = 800;
    private static uint SCR_HEIGHT = 600;

    // Câmera
    private static Vector3 _cameraPos = new Vector3(0.0f, 0.0f, 3.0f);
    private static Vector3 _cameraFront = new Vector3(0.0f, 0.0f, -1.0f);
    private static Vector3 _cameraUp = new Vector3(0.0f, 1.0f, 0.0f);

    // Tempo
    private static float _deltaTime = 0.0f;
    private static float _lastFrame = 0.0f;

    // Recursos da GPU e Estado de Renderização promovidos para eliminar Closures no Heap
    private unsafe static WindowHandle* _window;
    private static Shader _ourShader = null!;
    private static uint _vao;
    private static uint _vbo;
    private static uint _texture1;
    private static uint _texture2;

    private static readonly Vector3[] _cubePositions =
    {
        new Vector3( 0.0f,  0.0f,  0.0f),
        new Vector3( 2.0f,  5.0f, -15.0f),
        new Vector3(-1.5f, -2.2f, -2.5f),
        new Vector3(-3.8f, -2.0f, -12.3f),
        new Vector3( 2.4f, -0.4f, -3.5f),
        new Vector3(-1.7f,  3.0f, -7.5f),
        new Vector3( 1.3f, -2.0f, -2.5f),
        new Vector3( 1.5f,  2.0f, -2.5f),
        new Vector3( 1.5f,  0.2f, -1.5f),
        new Vector3(-1.3f,  1.0f, -1.5f)
    };

    private unsafe static void Main(string[] args)
    {
        _glfw = Glfw.GetApi();
        _gl = GL.GetApi(_glfw.GetProcAddress);

        _glfw.Init();
        _glfw.WindowHint(WindowHintInt.ContextVersionMajor, 3);
        _glfw.WindowHint(WindowHintInt.ContextVersionMinor, 3);
        _glfw.WindowHint(WindowHintOpenGlProfile.OpenGlProfile, OpenGlProfile.Core);

        if (OperatingSystem.IsMacOS())
        {
            _glfw.WindowHint(WindowHintBool.OpenGLForwardCompat, true);
        }

        _window = _glfw.CreateWindow((int)SCR_WIDTH, (int)SCR_HEIGHT, "Learn Silk.NET", null, null);

        if (_window == null)
        {
            Console.WriteLine("Falha ao criar a janela Silk.NET");
            _glfw.Terminate();
            return;
        }

        _glfw.GetWindowSize(_window, out int pWidth, out int pHeight);
        var vidmode = _glfw.GetVideoMode(_glfw.GetPrimaryMonitor());
        _glfw.SetWindowPos(_window, (vidmode->Width - pWidth) / 2, (vidmode->Height - pHeight) / 2);
        
        _glfw.MakeContextCurrent(_window);
        _glfw.SetFramebufferSizeCallback(_window, FramebufferSizeCallback);

        // Fixação determinística do callback
        _refreshCallback = WindowRefreshCallback;
        _glfw.SetWindowRefreshCallback(_window, _refreshCallback);

        _gl.Enable(EnableCap.DepthTest);

        _ourShader = new Shader(_gl, "src/camera.vs", "src/camera.fs");

        float[] vertices =
        {
            // positions           // texture coords
            -0.5f, -0.5f, -0.5f,   0.0f, 0.0f,
            -0.5f, -0.5f,  0.5f,   1.0f, 0.0f,
            -0.5f,  0.5f,  0.5f,   1.0f, 1.0f,
            -0.5f, -0.5f, -0.5f,   0.0f, 0.0f,
            -0.5f,  0.5f,  0.5f,   1.0f, 1.0f,
            -0.5f,  0.5f, -0.5f,   0.0f, 1.0f,
             0.5f, -0.5f,  0.5f,   0.0f, 0.0f,
             0.5f, -0.5f, -0.5f,   1.0f, 0.0f,
             0.5f,  0.5f, -0.5f,   1.0f, 1.0f,
             0.5f, -0.5f,  0.5f,   0.0f, 0.0f,
             0.5f,  0.5f, -0.5f,   1.0f, 1.0f,
             0.5f,  0.5f,  0.5f,   0.0f, 1.0f,
            -0.5f, -0.5f, -0.5f,   0.0f, 0.0f,
             0.5f, -0.5f, -0.5f,   1.0f, 0.0f,
             0.5f, -0.5f,  0.5f,   1.0f, 1.0f,
            -0.5f, -0.5f, -0.5f,   0.0f, 0.0f,
             0.5f, -0.5f,  0.5f,   1.0f, 1.0f,
            -0.5f, -0.5f,  0.5f,   0.0f, 1.0f,
            -0.5f,  0.5f,  0.5f,   0.0f, 0.0f,
             0.5f,  0.5f,  0.5f,   1.0f, 0.0f,
             0.5f,  0.5f, -0.5f,   1.0f, 1.0f,
            -0.5f,  0.5f,  0.5f,   0.0f, 0.0f,
             0.5f,  0.5f, -0.5f,   1.0f, 1.0f,
            -0.5f,  0.5f, -0.5f,   0.0f, 1.0f,
             0.5f, -0.5f, -0.5f,   0.0f, 0.0f,
            -0.5f, -0.5f, -0.5f,   1.0f, 0.0f,
            -0.5f,  0.5f, -0.5f,   1.0f, 1.0f,
             0.5f, -0.5f, -0.5f,   0.0f, 0.0f,
            -0.5f,  0.5f, -0.5f,   1.0f, 1.0f,
             0.5f,  0.5f, -0.5f,   0.0f, 1.0f,
            -0.5f, -0.5f,  0.5f,   0.0f, 0.0f,
             0.5f, -0.5f,  0.5f,   1.0f, 0.0f,
             0.5f,  0.5f,  0.5f,   1.0f, 1.0f,
            -0.5f, -0.5f,  0.5f,   0.0f, 0.0f,
             0.5f,  0.5f,  0.5f,   1.0f, 1.0f,
            -0.5f,  0.5f,  0.5f,   0.0f, 1.0f
        };

        _gl.GenVertexArrays(1, out _vao);
        _gl.GenBuffers(1, out _vbo);
        
        _gl.BindVertexArray(_vao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        fixed (float* buf = vertices)
        {
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (uint)(vertices.Length * sizeof(float)), buf, BufferUsageARB.StaticDraw);
        }

        _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), (void*)0);
        _gl.EnableVertexAttribArray(0);

        _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), (void*)(3 * sizeof(float)));
        _gl.EnableVertexAttribArray(1);
        
        // Texture 1
        _gl.GenTextures(1, out _texture1);
        _gl.BindTexture(TextureTarget.Texture2D, _texture1);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

        StbImage.stbi_set_flip_vertically_on_load(1);
        LoadTexture("res/textures/container.jpg", InternalFormat.Rgb, PixelFormat.Rgb);

        // Texture 2
        _gl.GenTextures(1, out _texture2);
        _gl.BindTexture(TextureTarget.Texture2D, _texture2);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

        LoadTexture("res/textures/awesomeface.png", InternalFormat.Rgba, PixelFormat.Rgba);

        _ourShader.Use();
        _ourShader.SetInt("texture1", 0);
        _ourShader.SetInt("texture2", 1);

        while (!_glfw.WindowShouldClose(_window))
        {
            RenderLoop();
            _glfw.PollEvents();
        }

        _gl.DeleteVertexArrays(1, ref _vao);
        _gl.DeleteBuffers(1, ref _vbo);
        _glfw.Terminate();
    }

    private static unsafe void LoadTexture(string path, InternalFormat internalFormat, PixelFormat pixelFormat)
    {
        using FileStream stream = File.OpenRead(path);
        ImageResult image = ImageResult.FromStream(stream, ColorComponents.Default);

        if (image.Data != null)
        {
            fixed (byte* ptr = image.Data)
            {
                _gl.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, (uint)image.Width, (uint)image.Height, 0, pixelFormat, PixelType.UnsignedByte, ptr);
            }
            _gl.GenerateMipmap(TextureTarget.Texture2D);
        }
        else
        {
            Console.WriteLine($"Falha ao carregar a textura: {path}");
        }
    }

    private static unsafe void WindowRefreshCallback(WindowHandle* window)
    {
        // Garante a re-renderização imediata durante o bloqueio de redimensionamento do Win32
        RenderLoop();
    }

    private static unsafe void RenderLoop()
    {
        float currentFrame = (float)_glfw.GetTime();
        _deltaTime = currentFrame - _lastFrame;
        _lastFrame = currentFrame;

        ProcessInput(_window);

        _gl.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, _texture1);
        _gl.ActiveTexture(TextureUnit.Texture1);
        _gl.BindTexture(TextureTarget.Texture2D, _texture2);

        _ourShader.Use();

        Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(
            fieldOfView:       MathHelper.DegreesToRadians(45.0f), 
            aspectRatio:       (float)SCR_WIDTH / (float)SCR_HEIGHT, 
            nearPlaneDistance: 0.1f, 
            farPlaneDistance:  100.0f
        );
        _ourShader.SetMat4("projection", projection);

        Matrix4x4 view = Matrix4x4.CreateLookAt(
            cameraPosition: _cameraPos, 
            cameraTarget:   _cameraPos + _cameraFront, 
            cameraUpVector: _cameraUp
        );
        _ourShader.SetMat4("view", view);
                    
        _gl.BindVertexArray(_vao);

        for (int i = 0; i < 10; i++)
        {
            Matrix4x4 model = Matrix4x4.Identity;
            float angle = 20.0f * i;
            model *= Matrix4x4.CreateFromAxisAngle(Vector3.Normalize(new Vector3(1.0f, 0.3f, 0.5f)), MathHelper.DegreesToRadians(angle));
            model *= Matrix4x4.CreateTranslation(_cubePositions[i]);
            _ourShader.SetMat4("model", model);

            _gl.DrawArrays(PrimitiveType.Triangles, 0, 36);
        }

        _glfw.SwapBuffers(_window);
    }

    private static unsafe void ProcessInput(WindowHandle* window)
    {
        if (_glfw.GetKey(window, Keys.Escape) == (int)InputAction.Press)
        {
            _glfw.SetWindowShouldClose(window, true);
        }

        float cameraSpeed = 2.5f * _deltaTime;

        if (_glfw.GetKey(window, Keys.W) == (int)InputAction.Press)
        {
            _cameraPos += cameraSpeed * _cameraFront;
        }
        if (_glfw.GetKey(window, Keys.S) == (int)InputAction.Press)
        {
            _cameraPos -= cameraSpeed * _cameraFront;
        }
        if (_glfw.GetKey(window, Keys.A) == (int)InputAction.Press)
        {
            _cameraPos -= cameraSpeed * Vector3.Normalize(Vector3.Cross(_cameraFront, _cameraUp));
        }
        if (_glfw.GetKey(window, Keys.D) == (int)InputAction.Press)
        {
            _cameraPos += cameraSpeed * Vector3.Normalize(Vector3.Cross(_cameraFront, _cameraUp));
        }
    }

    private static unsafe void FramebufferSizeCallback(WindowHandle* window, int width, int height)
    {
        _gl.Viewport(0, 0, (uint)width, (uint)height);
        SCR_WIDTH = (uint)width;
        SCR_HEIGHT = (uint)height;
    }
}
