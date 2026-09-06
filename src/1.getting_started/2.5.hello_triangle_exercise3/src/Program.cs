using Silk.NET.GLFW;
using Silk.NET.OpenGL;

namespace LearnSilkNET.src;

public class Program
{
    private static Glfw _glfw = null!;
    private static GL _gl = null!;

    // configurações
    private const uint SCR_WIDTH = 800;
    private const uint SCR_HEIGHT = 600;

    private static string _vertexShaderSource =
    @"
        layout (location = 0) in vec3 aPos;

        void main()
        {
            gl_Position = vec4(aPos.x, aPos.y, aPos.z, 1.0);
        }
    ";

    private static string _fragmentShader1Source =
    @"
        out vec4 FragColor;

        void main()
        {
            FragColor = vec4(1.0f, 0.5f, 0.2f, 1.0f);
        }
    ";

    private static string _fragmentShader2Source =
    @"
        out vec4 FragColor;

        void main()
        {
            FragColor = vec4(1.0f, 1.0f, 0.0f, 1.0f);
        }
    ";

    private unsafe static void Main(string[] args)
    {
        _glfw = Glfw.GetApi();
        _gl = GL.GetApi(_glfw.GetProcAddress);

        // glfw: inicializar e configurar
        // --------------------------------------------------
        _glfw.Init();
        _glfw.WindowHint(WindowHintInt.ContextVersionMajor, 3);
        _glfw.WindowHint(WindowHintInt.ContextVersionMinor, 3);
        _glfw.WindowHint(WindowHintOpenGlProfile.OpenGlProfile, OpenGlProfile.Core);

        if (OperatingSystem.IsMacOS())
        {
            _glfw.WindowHint(WindowHintBool.OpenGLForwardCompat, true);
        }

        // criação da janela glfw
        // --------------------------------------------------
        WindowHandle* window = _glfw.CreateWindow((int)SCR_WIDTH, (int)SCR_HEIGHT, "Learn Silk.NET", null, null);

        if (window == null)
        {
            Console.WriteLine("Falha ao criar a janela Silk.NET");
            _glfw.Terminate();
        }

        // Obtém o tamanho da janela passado para glfwCreateWindow
        _glfw.GetWindowSize(window, out int pWidth, out int pHeight);

        // Obtém a resolução do monitor principal
        var vidmode = _glfw.GetVideoMode(_glfw.GetPrimaryMonitor());

        // Centralizar a janela
        _glfw.SetWindowPos(
            window,
            (vidmode->Width - pWidth) / 2,
            (vidmode->Height - pHeight) / 2
        );

        _glfw.MakeContextCurrent(window);
        _glfw.SetFramebufferSizeCallback(window, FramebufferSizeCallback);

        // construir e compilar nosso programa de shader
        // --------------------------------------------------

        // desta vez, omitimos as verificações do log de compilação para facilitar a leitura (se você encontrar problemas, adicione as verificações de compilação; consulte os exemplos de código anteriores)
        uint vertexShader = _gl.CreateShader(ShaderType.VertexShader);

        uint fragmentShaderOrange = _gl.CreateShader(ShaderType.FragmentShader); // o primeiro shader de fragmento que gera a cor laranja
        uint fragmentShaderYellow = _gl.CreateShader(ShaderType.FragmentShader); // o segundo shader de fragmento que gera a cor amarela

        uint shaderProgramOrange = _gl.CreateProgram();
        uint shaderProgramYellow = _gl.CreateProgram(); // o segundo programa de shader

        _gl.ShaderSource(vertexShader, _vertexShaderSource);
        _gl.CompileShader(vertexShader);

        _gl.ShaderSource(fragmentShaderOrange, _fragmentShader1Source);
        _gl.CompileShader(fragmentShaderOrange);

        _gl.ShaderSource(fragmentShaderYellow, _fragmentShader2Source);
        _gl.CompileShader(fragmentShaderYellow);

        // vincular o primeiro objeto de programa
        _gl.AttachShader(shaderProgramOrange, vertexShader);
        _gl.AttachShader(shaderProgramOrange, fragmentShaderOrange);
        _gl.LinkProgram(shaderProgramOrange);

        // em seguida, vincule o segundo objeto de programa usando um shader de fragmento diferente (mas o mesmo shader de vértice)
        // isso é perfeitamente permitido, uma vez que as entradas e saídas de ambos os shaders — de vértice e de fragmento — são compatíveis.
        _gl.AttachShader(shaderProgramYellow, vertexShader);
        _gl.AttachShader(shaderProgramYellow, fragmentShaderYellow);
        _gl.LinkProgram(shaderProgramYellow);

        // configurar dados de vértice (e buffer(s)) e configurar atributos de vértice
        // --------------------------------------------------
        float[] firstTriangle =
        {
            -0.9f,  -0.5f, 0.0f,
             0.0f,  -0.5f, 0.0f,
            -0.45f,  0.5f, 0.0f
        };

        float[] secondTriangle =
        {
             0.0f,  -0.5f, 0.0f,
             0.9f,  -0.5f, 0.0f,
             0.45f,  0.5f, 0.0f
        };

        uint[] VAO = new uint[2], VBO = new uint[2];

        _gl.GenVertexArrays(2, VAO); // também podemos gerar múltiplos VAOs ou buffers ao mesmo tempo
        _gl.GenBuffers(2, VBO);

        // configuração do primeiro triângulo
        // --------------------------------------------------
        _gl.BindVertexArray(VAO[0]);

        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, VBO[0]);
        fixed (float* buf = firstTriangle)
        {
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (uint)(firstTriangle.Length * sizeof(float)), buf, BufferUsageARB.StaticDraw);
        }

        _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*)0); // Os atributos de vértice permanecem os mesmos
        _gl.EnableVertexAttribArray(0);
        
        // _gl.BindVertexArray(0); // não é necessário desfazer a vinculação, pois vinculamos diretamente um VAO diferente nas próximas linhas

        // configuração do segundo triângulo
        // --------------------------------------------------
        _gl.BindVertexArray(VAO[1]); // observe que agora vinculamos a um VAO diferente

        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, VBO[1]); // e um VBO diferente
        fixed (float* buf = secondTriangle)
        {
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (uint)(secondTriangle.Length * sizeof(float)), buf, BufferUsageARB.StaticDraw);
        }

        _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*)0); // como os dados dos vértices estão compactados, também podemos especificar 0 como o stride do atributo de vértice para deixar o OpenGL determiná-lo
        _gl.EnableVertexAttribArray(0);
        
        // _gl.BindVertexArray(0); // também não é estritamente necessário, mas cuidado com chamadas que possam afetar VAOs enquanto este estiver vinculado (como vincular *element buffer objects* ou habilitar/desabilitar atributos de vértice)

        // descomente esta chamada para desenhar polígonos em wireframe.
        // _gl.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);

        // loop de renderização
        // --------------------------------------------------
        while (!_glfw.WindowShouldClose(window))
        {
            // input
            // --------------------------------------------------
            ProcessInput(window);

            // render
            // --------------------------------------------------
            _gl.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
            _gl.Clear(ClearBufferMask.ColorBufferBit);

            // agora, ao desenhar o triângulo, usamos primeiro o shader de vértice e o shader de fragmento laranja do primeiro programa
            _gl.UseProgram(shaderProgramOrange);

            // desenha o primeiro triângulo usando os dados do primeiro VAO
            _gl.BindVertexArray(VAO[0]);
            _gl.DrawArrays(PrimitiveType.Triangles, 0, 3); // esta chamada deve gerar um triângulo laranja

            // então, desenhamos o segundo triângulo usando os dados do segundo VAO
            // ao desenhar o segundo triângulo, queremos usar um programa de shader diferente; por isso, alternamos para o programa de shader que utiliza nosso shader de fragmento amarelo.
            _gl.UseProgram(shaderProgramYellow);
            _gl.BindVertexArray(VAO[1]);
            _gl.DrawArrays(PrimitiveType.Triangles, 0, 3); // esta chamada deve gerar um triângulo amarelo

            // glfw: troca os buffers e processa eventos de E/S (teclas pressionadas/liberadas, movimento do mouse, etc.)
            // --------------------------------------------------
            _glfw.SwapBuffers(window);
            _glfw.PollEvents();
        }

        // opcional: desalocar todos os recursos assim que não forem mais necessários:
        // --------------------------------------------------
        _gl.DeleteVertexArrays(2, VAO);
        _gl.DeleteBuffers(2, VBO);
        _gl.DeleteProgram(shaderProgramOrange);
        _gl.DeleteProgram(shaderProgramYellow);

        // glfw: encerra, liberando todos os recursos do GLFW alocados anteriormente.
        // --------------------------------------------------
        _glfw.Terminate();
    }

    // process all input: query GLFW whether relevant keys are pressed/released this frame and react accordingly
    // --------------------------------------------------
    private static unsafe void ProcessInput(WindowHandle* window)
    {
        if (_glfw.GetKey(window, Keys.Escape) == (int)InputAction.Press)
        {
            _glfw.SetWindowShouldClose(window, true);
        }
    }

    // glfw: sempre que o tamanho da janela é alterado (pelo SO ou por redimensionamento do usuário), esta função de callback é executada
    // --------------------------------------------------
    private static unsafe void FramebufferSizeCallback(WindowHandle* window, int width, int height)
    {
        // certifique-se de que a viewport corresponda às novas dimensões da janela; observe que a largura e
        // a altura serão significativamente maiores do que as especificadas em telas Retina.
        _gl.Viewport(0, 0, (uint)width, (uint)height);
    }
}
