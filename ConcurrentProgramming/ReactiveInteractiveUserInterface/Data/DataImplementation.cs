//____________________________________________________________________________________________________________________________________
//
//  Copyright (C) 2024, Mariusz Postol LODZ POLAND.
//
//  To be in touch join the community by pressing the `Watch` button and get started commenting using the discussion panel at
//
//  https://github.com/mpostol/TP/discussions/182
//
//_____________________________________________________________________________________________________________________________________

using System;
using System.Diagnostics;

namespace TP.ConcurrentProgramming.Data
{
  internal class DataImplementation : DataAbstractAPI
  {
    #region ctor

    public DataImplementation()
    {
      MoveTimer = new Timer(Move, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(1000/60));
    }

    #endregion ctor

    private const double tableWidth = 400.0;
    private const double tableHeight = 400.0;
    private const double ballRadius = 10.0; // smelly, definicja średnicy jest w warstwach wyżej

    #region DataAbstractAPI

    public override void Start(int numberOfBalls, Action<IVector, IBall> upperLayerHandler)
    {
      if (Disposed)
        throw new ObjectDisposedException(nameof(DataImplementation));
      if (upperLayerHandler == null)
        throw new ArgumentNullException(nameof(upperLayerHandler));
      Random random = new Random();
      for (int i = 0; i < numberOfBalls; i++)
      {
        Vector startingPosition = new(random.Next(100, (int)tableWidth - 100), random.Next(100, (int)tableHeight - 100));
        double angle = 2 * Math.PI * random.NextDouble();
        double speed = 2.0;
        double vx = speed * Math.Cos(angle);
        double vy = speed * Math.Sin(angle);
        Vector initialVelocity = new Vector(vx, vy);

        Ball newBall = new(startingPosition, initialVelocity);
        upperLayerHandler(startingPosition, newBall);
        BallsList.Add(newBall);
      }
    }

    #endregion DataAbstractAPI

    #region IDisposable

    protected virtual void Dispose(bool disposing)
    {
      if (!Disposed)
      {
        if (disposing)
        {
          MoveTimer.Dispose();
          BallsList.Clear();
        }
        Disposed = true;
      }
      else
        throw new ObjectDisposedException(nameof(DataImplementation));
    }

    public override void Dispose()
    {
      // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
      Dispose(disposing: true);
      GC.SuppressFinalize(this);
    }

    #endregion IDisposable

    #region private

    //private bool disposedValue;
    private bool Disposed = false;

    private readonly Timer MoveTimer;
    private Random RandomGenerator = new();
    private List<Ball> BallsList = [];

    private void Move(object? x)
    {
        foreach (Ball ball in BallsList)
        {
            ball.Move(new Vector(ball.Velocity.x, ball.Velocity.y));
            Vector position = ball.Position;
            if ((position.x - ballRadius <= 0 && ball.Velocity.x < 0) || (position.x + ballRadius >= tableWidth && ball.Velocity.x > 0))
            {
                ball.Velocity = new Vector(-ball.Velocity.x, ball.Velocity.y);
            }
            if ((position.y - ballRadius <= 0 && ball.Velocity.y < 0) || (position.y + ballRadius >= tableHeight && ball.Velocity.y > 0))
            {
                ball.Velocity = new Vector(ball.Velocity.x, -ball.Velocity.y);
            }
        }
        for (int i = 0; i < BallsList.Count; i++)
        {
            for (int j = i + 1; j < BallsList.Count; j++)
            {
                Ball ball1 = BallsList[i];
                Ball ball2 = BallsList[j];

                Vector delta = new Vector(
                    ball2.Position.x - ball1.Position.x,
                    ball2.Position.y - ball1.Position.y);

                double distance = Math.Sqrt(delta.x * delta.x + delta.y * delta.y);

                if (distance < 2 * ballRadius) // kolizja
                {
                    // wyznaczenie wektora normalnego (kierunek kolizji)
                    Vector normal = new Vector(delta.x / distance, delta.y / distance);
                    Vector tangent = new Vector(-normal.y, normal.x); // wektor styczny
                    double v1n = ball1.Velocity.x * normal.x + ball1.Velocity.y * normal.y; // prędkość w kierunku normalnym
                    double v2n = ball2.Velocity.x * normal.x + ball2.Velocity.y * normal.y;
                    double v1t = ball1.Velocity.x * tangent.x + ball1.Velocity.y * tangent.y; // prędkość w kierunku stycznym
                    double v2t = ball2.Velocity.x * tangent.x + ball2.Velocity.y * tangent.y;
                    // nowa prędkość w kierunku normalnym
                    // składowe styczne pozostają niezmienione a normalne się wymieniają
                    double newV1x = (v2n * normal.x) + (v1t * tangent.x);
                    double newV1y = (v2n * normal.y) + (v1t * tangent.y);
                    double newV2x = (v1n * normal.x) + (v2t * tangent.x);
                    double newV2y = (v1n * normal.y) + (v2t * tangent.y);
                    ball1.Velocity = new Vector(newV1x, newV1y);
                    ball2.Velocity = new Vector(newV2x, newV2y);

                    // jeśli kulki są " w sobie" to przesuwamy je do pozycji styku
                    double overlap = 2 * ballRadius - distance;
                    ball1.Move(new Vector(-normal.x * overlap / 2, -normal.y * overlap / 2));
                    ball2.Move(new Vector(normal.x * overlap / 2, normal.y * overlap / 2));
                }
            }
        }
    }
    #endregion private

    #region TestingInfrastructure

    [Conditional("DEBUG")]
    internal void CheckBallsList(Action<IEnumerable<IBall>> returnBallsList)
    {
      returnBallsList(BallsList);
    }

    [Conditional("DEBUG")]
    internal void CheckNumberOfBalls(Action<int> returnNumberOfBalls)
    {
      returnNumberOfBalls(BallsList.Count);
    }

    [Conditional("DEBUG")]
    internal void CheckObjectDisposed(Action<bool> returnInstanceDisposed)
    {
      returnInstanceDisposed(Disposed);
    }

    #endregion TestingInfrastructure
  }
}