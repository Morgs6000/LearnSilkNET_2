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

// BallObject mantém o estado do objeto Ball, herdando
// dados de estado relevantes de GameObject. Contém funcionalidades
// extras específicas para o objeto de bola do Breakout, que
// eram específicas demais para constar apenas em GameObject.
public class BallObject : GameObject
{
    // estado da bola
    public float Radius;
    public bool Stuck;
    public bool Sticky, PassThrough;

    // construtor(es)
    public BallObject(GL gl) : base(gl)
    {
        Radius = 12.5f;

        Stuck = true;

        Sticky = false;
        PassThrough = false;
    }

    public BallObject(Vector2 pos, float radius, Vector2 velocity, Texture2D sprite) : base(pos, new Vector2(radius * 2.0f, radius * 2.0f), sprite, new Vector3(1.0f), velocity)
    {
        Radius = radius;

        Stuck = true;

        Sticky = false;
        PassThrough = false;
    }

    // move a bola, mantendo-a dentro dos limites da janela (exceto a borda inferior); retorna a nova posição
    public Vector2 Move(float dt, uint window_width)
    {
        // se não estiver fixado no tabuleiro do jogador
        if (!Stuck)
        {
            // mover a bola
            Position += Velocity * dt;

            // então, verifique se está fora dos limites da janela e, se estiver, inverta a velocidade e restaure a posição correta
            if (Position.X <= 0.0f)
            {
                Velocity.X = -Velocity.X;
                Position.X = 0.0f;
            }
            else if (Position.X + Size.X >= window_width)
            {
                Velocity.X = -Velocity.X;
                Position.X = window_width - Size.X;
            }

            if (Position.Y <= 0.0f)
            {
                Velocity.Y = -Velocity.Y;
                Position.Y = 0.0f;
            }
        }

        return Position;
    }

    // redefine a bola para o estado original com a posição e a velocidade especificadas
    public void Reset(Vector2 position, Vector2 velocity)
    {
        Position = position;
        Velocity = velocity;
        
        Stuck = true;

        Sticky = false;
        PassThrough = false;
    }
}
