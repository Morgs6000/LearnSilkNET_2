/*******************************************************************
** This code is part of Breakout.
**
** Breakout is free software: you can redistribute it and/or modify
** it under the terms of the CC BY 4.0 license as published by
** Creative Commons, either version 4 of the License, or (at your
** option) any later version.
******************************************************************/

using System.Numerics;
using Silk.NET.OpenGL;

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
    // estado do jogo
    public GameState State;
    public bool[] Keys = new bool[1024];
    public uint Widht, Height;

    // Dados de estado relacionados ao jogo
    public SpriteRenderer Renderer = null!;

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
        ResourceManager.LoadTexture(gl, "res/textures/awesomeface.png", true, "face");
    }

    // loop do jogo
    public void ProcessInput(float dt)
    {
        
    }

    public void Update(float dt)
    {
        
    }

    public void Render()
    {
        Renderer.DrawSprite(ResourceManager.GetTexture("face"), new Vector2(200.0f, 200.0f), new Vector2(300.0f, 400.0f), 45.0f, new Vector3(0.0f, 1.0f, 0.0f));
    }
}
