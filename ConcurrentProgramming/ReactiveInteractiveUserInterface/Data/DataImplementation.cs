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
using System.Threading;

namespace TP.ConcurrentProgramming.Data
{
  internal class DataImplementation : DataAbstractAPI
  {
    
    #region DataAbstractAPI
    
    public override void Start(int numberOfBalls, Action<IVector, IBall> upperLayerHandler)
    {
      if (Disposed)
        throw new ObjectDisposedException(nameof(DataImplementation));
      if (upperLayerHandler == null)
        throw new ArgumentNullException(nameof(upperLayerHandler));
      lock (BallsList)
      {
				foreach (Ball ball in BallsList)
				{
          ball.StopThread();
				}
				BallsList.Clear();
        Random random = new Random();
        for (int i = 0; i < numberOfBalls; i++)
        {
          Vector startingPosition = new(random.Next(100, (int)tableWidth - 100), random.Next(100, (int)tableHeight - 100));
          double angle = 2 * Math.PI * random.NextDouble();
          double speed = 1.5;
          double vx = speed * Math.Cos(angle);
          double vy = speed * Math.Sin(angle);
          Vector initialVelocity = new Vector(vx, vy);

          Ball newBall = new(startingPosition, initialVelocity);
          upperLayerHandler(startingPosition, newBall);
          BallsList.Add(newBall);
        }
				foreach (Ball ball in BallsList)
				{
					ball.StartThread(this.Move);
				}
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
          lock(BallsList)
          {
						foreach (Ball ball in BallsList)
						{
							ball.StopThread();
						}
						BallsList.Clear();
					}
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

    private List<Ball> BallsList = [];

    private const double tableWidth = 400.0;
    private const double tableHeight = 400.0;
    private const double ballRadius = 10.0;

    private void Move(Ball ball)
    {
      lock (BallsList) {
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
        for (int i = 0; i < BallsList.Count; i++)
        {
          Ball otherBall = BallsList[i];
          if(otherBall == ball)
          {
            continue;
          }
          Vector delta = new Vector(
            otherBall.Position.x - ball.Position.x,
            otherBall.Position.y - ball.Position.y);

          double distance = Math.Sqrt(delta.x * delta.x + delta.y * delta.y);

          if (distance < 2 * ballRadius) // kolizja
          {
            // wyznaczenie wektora normalnego (kierunek kolizji)
            Vector normal = new Vector(delta.x / distance, delta.y / distance);
            Vector tangent = new Vector(-normal.y, normal.x); // wektor styczny
            double v1n = ball.Velocity.x * normal.x + ball.Velocity.y * normal.y; // prędkość w kierunku normalnym
            double v2n = otherBall.Velocity.x * normal.x + otherBall.Velocity.y * normal.y;
            double v1t = ball.Velocity.x * tangent.x + ball.Velocity.y * tangent.y; // prędkość w kierunku stycznym
            double v2t = otherBall.Velocity.x * tangent.x + otherBall.Velocity.y * tangent.y;
						// uwzglednianie mas
						double m1 = ball.mass;
            double m2 = otherBall.mass;
            double newV1n = (v1n * (m1 - m2) + 2 * m2 * v2n) / (m1 + m2);
						double newV2n = (v2n * (m2 - m1) + 2 * m1 * v1n) / (m1 + m2);
						// nowa prędkość w kierunku normalnym
						double newV1x = (newV1n * normal.x) + (v1t * tangent.x);
            double newV1y = (newV1n * normal.y) + (v1t * tangent.y);
            double newV2x = (newV2n * normal.x) + (v2t * tangent.x);
            double newV2y = (newV2n * normal.y) + (v2t * tangent.y);
            ball.Velocity = new Vector(newV1x, newV1y);
            otherBall.Velocity = new Vector(newV2x, newV2y);

            // jeśli kulki są " w sobie" to przesuwamy je do pozycji styku
            double overlap = 2 * ballRadius - distance;
            ball.Move(new Vector(-normal.x * overlap / 2, -normal.y * overlap / 2));
            otherBall.Move(new Vector(normal.x * overlap / 2, normal.y * overlap / 2));
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