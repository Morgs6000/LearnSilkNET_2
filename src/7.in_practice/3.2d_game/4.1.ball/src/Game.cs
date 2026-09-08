/*******************************************************************
** This code is part of Breakout.
**
** Breakout is free software: you can redistribute it and/or modify
** it under the terms of the CC BY 4.0 license as published by
** Creative Commons, either version 4 of the License, or (at your
** option) any later version.
******************************************************************/

using System.Numerics;
using System.Security.Cryptography.X509Certificates;
using Silk.NET.OpenGL;
using GLFW_KEY = Silk.NET.GLFW.Keys;

namespace Breakout.src;

// Representa o estado atual do jogo
public enum GameState
{
    GAME_ACTIVE,
    GAME_MENU,
    GAME_WIN
}

// A classe Game encapsula todo o estado e a funcionalidade relacionados ao jogo.
// Ela reúne todos os dados do jogo em uma única classe para
// facilitar o acesso aos componentes e o gerenciamento.
public class Game : IDisposable
{
    // Tamanho inicial da raquete do jogador
    public static Vector2 PLAYER_SIZE = new Vector2(100.0f, 20.0f);

    // Velocidade inicial da raquete do jogador
    public const float PLAYER_VELOCITY = 500.0f;

    // Velocidade inicial da bola
    public static Vector2 INITIAL_BALL_VELOCITY = new Vector2(100.0f, -350.0f);

    // Raio do objeto bola
    public const float BALL_RADIUS = 12.5f;
    
    // estado do jogo
    public GameState State;
    public bool[] Keys = new bool[1024];
    public uint Widht, Height;
    public List<GameLevel> Levels = [];
    public int Level;

    // Dados de estado relacionados ao jogo
    public SpriteRenderer Renderer = null!;
    public GameObject Player = null!;
    public BallObject Ball = null!;

    // construtor
    public Game(uint width, uint height)
    {
        State = GameState.GAME_ACTIVE;
        
        Widht = width;
        Height = height;
    }

    // desconstrutor
    public void Dispose()
    {
        Renderer.Dispose();
    }

    // inicializar o estado do jogo (carregar todos os shaders/texturas/níveis)
    public void Init(GL gl)
    {
        // carregar shaders
        ResourceManager.LoadShader(gl, "src/sprite.vs", "src/sprite.fs", null, "sprite");

        // configurar shaders
        Matrix4x4 projection = Matrix4x4.CreateOrthographicOffCenter(
            left:        0.0f, 
            right:       (float)Widht, 
            bottom:      (float)Height, 
            top:         0.0f, 
            zNearPlane: -1.0f, 
            zFarPlane:   1.0f
        );

        ResourceManager.GetShader("sprite").Use().SetInteger("image", 0);
        ResourceManager.GetShader("sprite").SetMatrix4("projection", projection);

        // definir controles específicos de renderização
        Renderer = new SpriteRenderer(gl, ResourceManager.GetShader("sprite"));

        // carregar texturas
        ResourceManager.LoadTexture(gl, "res/textures/background.jpg", false, "background");
        ResourceManager.LoadTexture(gl, "res/textures/awesomeface.png", true, "face");
        ResourceManager.LoadTexture(gl, "res/textures/block.png", false, "block");
        ResourceManager.LoadTexture(gl, "res/textures/block_solid.png", false, "block_solid");
        ResourceManager.LoadTexture(gl, "res/textures/paddle.png", true, "paddle");

        // carregar níveis
        GameLevel one = new GameLevel(); one.Load("src/levels/one.lvl", Widht, Height / 2);
        GameLevel two = new GameLevel(); two.Load("src/levels/two.lvl", Widht, Height / 2);
        GameLevel three = new GameLevel(); three.Load("src/levels/three.lvl", Widht, Height / 2);
        GameLevel four = new GameLevel(); four.Load("src/levels/four.lvl", Widht, Height / 2);

        Levels.Add(one);
        Levels.Add(two);
        Levels.Add(three);
        Levels.Add(four);

        Level = 0;

        // configurar objetos do jogo
        Vector2 playerPos = new Vector2(Widht / 2.0f - PLAYER_SIZE.X / 2.0f, Height - PLAYER_SIZE.Y);
        Player = new GameObject(playerPos, PLAYER_SIZE, ResourceManager.GetTexture("paddle"));

        Vector2 ballPos = playerPos + new Vector2(PLAYER_SIZE.X / 2.0f - BALL_RADIUS, -BALL_RADIUS * 2.0f);
        Ball = new BallObject(ballPos, BALL_RADIUS, INITIAL_BALL_VELOCITY, ResourceManager.GetTexture("face"));
    }

    // loop do jogo
    public void ProcessInput(float dt)
    {
        if (State == GameState.GAME_ACTIVE)
        {
            float velocity = PLAYER_VELOCITY * dt;

            // mover o tabuleiro do jogador
            if (Keys[(int)GLFW_KEY.A])
            {
                if (Player.Position.X >= 0.0f)
                {
                    Player.Position.X -= velocity;

                    if (Ball.Stuck)
                    {
                        Ball.Position.X -= velocity;
                    }
                }
            }
            if (Keys[(int)GLFW_KEY.D])
            {
                if (Player.Position.X <= Widht - Player.Size.X)
                {
                    Player.Position.X += velocity;

                    if (Ball.Stuck)
                    {
                        Ball.Position.X += velocity;
                    }
                }
            }
            if (Keys[(int)GLFW_KEY.Space])
            {
                Ball.Stuck = false;
            }
        }
    }

    public void Update(float dt)
    {
        Ball.Move(dt, Widht);
    }

    public void Render()
    {
        if (State == GameState.GAME_ACTIVE)
        {
            // desenhar fundo
            Renderer.DrawSprite(ResourceManager.GetTexture("background"), new Vector2(0.0f, 0.0f), new Vector2(Widht, Height), 0.0f);

            // desenhar nível
            Levels[Level].Draw(Renderer);

            // desenhar jogador
            Player.Draw(Renderer);

            Ball.Draw(Renderer);
        }
    }
}
