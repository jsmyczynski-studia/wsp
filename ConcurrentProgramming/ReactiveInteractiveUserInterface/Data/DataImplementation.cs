﻿//____________________________________________________________________________________________________________________________________
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
    public override IVector CreateVector(double x, double y)
    {
        return new Vector(x, y);
    }
    public override void ChangePos(IBall ib, IVector d)
    {
        Ball b = (Ball)ib;
        Vector delta = new Vector(d.x, d.y);
        b.Move(delta);
    }
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
          double speed = 1.0;
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
    private void Move(Ball ball)
    {
      lock (BallsList)
      {
        ball.Move(new Vector(ball.Velocity.x, ball.Velocity.y));  
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