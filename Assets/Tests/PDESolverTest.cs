using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine;

[TestFixture]
public class PDESolverTest
{

    #region Fields

    private float unitError = 0.9996f;

    #endregion

    #region Properties

    public static List<(int, float, MatterState, float, int, float, float)> DensityTestParameters
    {
        get
        {
            return GenerateDensityTestParameters();
        }
    }

    public static List<(int, float, MatterState, float, int, float, float, float)> VelocityTestParameters
    {
        get
        {
            return GenerateVelocityTestParameters();
        }
    }

    #endregion

    #region Test parameter generation

    private static List<(int, float, MatterState, float, int, float, float)> GenerateDensityTestParameters()
    {
        List<(int, float, MatterState, float, int, float, float)>  densityTestParameters = new List<(int, float, MatterState, float, int, float, float)>();

        int[] gridSizeValues = { 20, 60 };
        float[] timeStepValues = { 0.002f, 0.2f };
        MatterState[] matterStateValues = { MatterState.FLUID, MatterState.GAS };
        float[] viscosityValues = { 0.000002f, 0.0002f };
        int[] stepCountValues = { 20, 100 };
        float[] gravityValues = { -20, 20 };
        float[] densityValues = { 0.1f, 2 };

        foreach (int gridSize in gridSizeValues)
        {
            foreach (float timeStep in timeStepValues)
            {
                foreach (MatterState matterState in matterStateValues)
                {
                    foreach (float viscosity in viscosityValues)
                    {
                        foreach (int stepCount in stepCountValues)
                        {
                            foreach (float gravity in gravityValues)
                            {
                                foreach (float density in densityValues)
                                {
                                    densityTestParameters.Add((gridSize, timeStep, matterState, viscosity, stepCount, gravity, density));
                                }
                            }
                        }
                    }
                }
            }
        }

        return densityTestParameters;
    }

    private static List<(int, float, MatterState, float, int, float, float, float)> GenerateVelocityTestParameters()
    {
        List<(int, float, MatterState, float, int, float, float, float)>  velocityTestParameters = new List<(int, float, MatterState, float, int, float, float, float)>();

        int[] gridSizeValues = { 10, 100 };
        float[] timeStepValues = { 0.002f, 0.5f };
        MatterState[] matterStateValues = { MatterState.FLUID, MatterState.GAS };
        float[] viscosityValues = { 0.000002f, 0.0002f };
        int[] stepCountValues = { 20, 100 };
        float[] gravityValues = { -20, 20 };
        float[] velocityXValues = { -1.2f, 1.2f };
        float[] velocityYValues = { -1.2f, 1.2f };

        foreach (int gridSize in gridSizeValues)
        {
            foreach (float timeStep in timeStepValues)
            {
                foreach (MatterState matterState in matterStateValues)
                {
                    foreach (float viscosity in viscosityValues)
                    {
                        foreach (int stepCount in stepCountValues)
                        {
                            foreach (float gravity in gravityValues)
                            {
                                foreach (float velocityX in velocityXValues)
                                {
                                    foreach (float velocityY in velocityYValues)
                                    {
                                        velocityTestParameters.Add((gridSize, timeStep, matterState, viscosity, stepCount, gravity, velocityX, velocityY));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        return velocityTestParameters;
    }

    #endregion

    #region Tests

    [TestCaseSource(nameof(DensityTestParameters))]
    public void DensityAdditionTest((int gridSize, float timeStep, MatterState matterState, float viscosity, int stepCount, float gravity, float value) param_)
    {
        int gridSize = param_.gridSize;
        float timeStep = param_.timeStep;
        MatterState matterState = param_.matterState;
        float viscosity = param_.viscosity;
        int stepCount = param_.stepCount;
        float gravity = param_.gravity;
        float value = param_.value;

        PDESolver solver = new PDESolver(gridSize, timeStep, matterState, viscosity, stepCount, gravity, new WallType[gridSize + 2, gridSize + 2]);

        System.Random random = new System.Random();
        int addX = random.Next(1, gridSize - 1);
        int addY = random.Next(1, gridSize - 1);

        solver.UpdateVelocity();
        solver.UpdateDensity(value, addX, addY);

        Assert.GreaterOrEqual(solver.Grid.Density[addX, addY], value - value * unitError);
    }

    [TestCaseSource(nameof(DensityTestParameters))]
    public void SystemDensityTestFromInitialValue((int gridSize, float timeStep, MatterState matterState, float viscosity, int stepCount, float gravity, float value) param_)
    {
        int gridSize = param_.gridSize;
        float timeStep = param_.timeStep;
        MatterState matterState = param_.matterState;
        float viscosity = param_.viscosity;
        int stepCount = param_.stepCount;
        float gravity = param_.gravity;
        float value = param_.value;

        PDESolver solver = new PDESolver(gridSize, timeStep, matterState, viscosity, stepCount, gravity, new WallType[gridSize + 2, gridSize + 2]);

        System.Random random = new System.Random();
        int addX = random.Next(1, gridSize - 1);
        int addY = random.Next(1, gridSize - 1);

        solver.UpdateVelocity();
        solver.UpdateDensity(value, addX, addY);

        Assert.GreaterOrEqual(solver.Grid.Density.Cast<float>().Sum(), value - value * unitError);
    }

    [TestCaseSource(nameof(DensityTestParameters))]
    public void SystemDensityTestFromPreviousValue((int gridSize, float timeStep, MatterState matterState, float viscosity, int stepCount, float gravity, float value) param_)
    {
        int gridSize = param_.gridSize;
        float timeStep = param_.timeStep;
        MatterState matterState = param_.matterState;
        float viscosity = param_.viscosity;
        int stepCount = param_.stepCount;
        float gravity = param_.gravity;
        float value = param_.value;

        PDESolver solver = new PDESolver(gridSize, timeStep, matterState, viscosity, stepCount, gravity, new WallType[gridSize + 2, gridSize + 2]);

        System.Random random = new System.Random();
        int addX = random.Next(1, gridSize - 1);
        int addY = random.Next(1, gridSize - 1);

        solver.UpdateVelocity();
        solver.UpdateDensity(value, addX, addY);

        float previousDensity = solver.Grid.Density.Cast<float>().Sum();

        solver.UpdateVelocity();
        solver.UpdateDensity(value, addX, addY);

        Assert.GreaterOrEqual(solver.Grid.Density.Cast<float>().Sum(), previousDensity);
    }

    [TestCaseSource(nameof(DensityTestParameters))]
    public void LongTermSystemDensityChangeTest((int gridSize, float timeStep, MatterState matterState, float viscosity, int stepCount, float gravity, float value) param_)
    {
        int gridSize = param_.gridSize;
        float timeStep = param_.timeStep;
        MatterState matterState = param_.matterState;
        float viscosity = param_.viscosity;
        int stepCount = param_.stepCount;
        float gravity = param_.gravity;
        float value = param_.value;

        PDESolver solver = new PDESolver(gridSize, timeStep, matterState, viscosity, stepCount, gravity, new WallType[gridSize + 2, gridSize + 2]);

        System.Random random = new System.Random();
        int addX = random.Next(1, gridSize - 1);
        int addY = random.Next(1, gridSize - 1);
        float previousDensity = 0;

        for (int i = 0; i < 60; ++i)
        {
            solver.UpdateVelocity();
            solver.UpdateDensity(value, addX, addY);
            Assert.GreaterOrEqual(solver.Grid.Density.Cast<float>().Sum(), previousDensity - unitError * 3);

            previousDensity = solver.Grid.Density.Cast<float>().Sum();
            random = new System.Random();
            addX = random.Next(1, gridSize - 1);
            addY = random.Next(1, gridSize - 1);
        }
    }

    [TestCaseSource(nameof(DensityTestParameters))]
    public void DensityConservationTest((int gridSize, float timeStep, MatterState matterState, float viscosity, int stepCount, float gravity, float value) param_)
    {
        int gridSize = param_.gridSize;
        float timeStep = param_.timeStep;
        MatterState matterState = param_.matterState;
        float viscosity = param_.viscosity;
        int stepCount = param_.stepCount;
        float gravity = param_.gravity;
        float value = param_.value;

        PDESolver solver = new PDESolver(gridSize, timeStep, matterState, viscosity, stepCount, gravity, new WallType[gridSize + 2, gridSize + 2]);

        System.Random random = new System.Random();
        int addX = random.Next(1, gridSize - 1);
        int addY = random.Next(1, gridSize - 1);

        solver.UpdateVelocity();
        solver.UpdateDensity(value, addX, addY);

        for (int i = 0; i < 59; ++i)
        {
            solver.UpdateVelocity();
            solver.UpdateDensity();
        }
        Assert.GreaterOrEqual(solver.Grid.Density.Cast<float>().Sum(), value - value * unitError * 3);
    }

    [TestCaseSource(nameof(VelocityTestParameters))]
    public void VelocityAdditionTestWithPoint((int gridSize, float timeStep, MatterState matterState, float viscosity, int stepCount, float gravity, float xValue, float yValue) param_)
    {
        int gridSize = param_.gridSize;
        float timeStep = param_.timeStep;
        MatterState matterState = param_.matterState;
        float viscosity = param_.viscosity;
        int stepCount = param_.stepCount;
        float gravity = param_.gravity;
        float xValue = param_.xValue;
        float yValue = param_.yValue;

        PDESolver solver = new PDESolver(gridSize, timeStep, matterState, viscosity, stepCount, gravity, new WallType[gridSize + 2, gridSize + 2]);

        System.Random random = new System.Random();
        int addX = random.Next(1, gridSize - 1);
        int addY = random.Next(1, gridSize - 1);

        (int, int)[] pointPosition = { (addX, addY) };
        solver.UpdateVelocity(xValue, yValue, pointPosition);
        solver.UpdateDensity();

        if(xValue - xValue * unitError > 0)
        {
            Assert.GreaterOrEqual(solver.Grid.VelocityX[addX, addY], xValue - xValue * unitError);
        }
        else
        {
            Assert.LessOrEqual(solver.Grid.VelocityX[addX, addY], xValue - xValue * unitError);
        }

        if(yValue - yValue * unitError > 0)
        {
            Assert.GreaterOrEqual(solver.Grid.VelocityY[addX, addY], yValue - yValue * unitError);
        }
        else
        {
            Assert.LessOrEqual(solver.Grid.VelocityY[addX, addY], yValue - yValue * unitError);
        }
    }

    [TestCaseSource(nameof(VelocityTestParameters))]
    public void VelocityAdditionTestWithSquare((int gridSize, float timeStep, MatterState matterState, float viscosity, int stepCount, float gravity, float xValue, float yValue) param_)
    {
        int gridSize = param_.gridSize;
        float timeStep = param_.timeStep;
        MatterState matterState = param_.matterState;
        float viscosity = param_.viscosity;
        int stepCount = param_.stepCount;
        float gravity = param_.gravity;
        float xValue = param_.xValue;
        float yValue = param_.yValue;

        PDESolver solver = new PDESolver(gridSize, timeStep, matterState, viscosity, stepCount, gravity, new WallType[gridSize + 2, gridSize + 2]);

        (int, int)[] squarePosition = { (5, 5), (6, 5), (5, 4), (6, 4) };
        solver.UpdateVelocity(xValue, yValue, squarePosition);
        solver.UpdateDensity();

        if (xValue - xValue * unitError > 0)
        {
            Assert.GreaterOrEqual(solver.Grid.VelocityX[5, 5], xValue - xValue * unitError);
            Assert.GreaterOrEqual(solver.Grid.VelocityX[6, 5], xValue - xValue * unitError);
            Assert.GreaterOrEqual(solver.Grid.VelocityX[5, 4], xValue - xValue * unitError);
            Assert.GreaterOrEqual(solver.Grid.VelocityX[6, 4], xValue - xValue * unitError);
        }
        else
        {
            Assert.LessOrEqual(solver.Grid.VelocityX[5, 5], xValue - xValue * unitError);
            Assert.LessOrEqual(solver.Grid.VelocityX[6, 5], xValue - xValue * unitError);
            Assert.LessOrEqual(solver.Grid.VelocityX[5, 4], xValue - xValue * unitError);
            Assert.LessOrEqual(solver.Grid.VelocityX[6, 4], xValue - xValue * unitError);
        }

        if (yValue - yValue * unitError > 0)
        {
            Assert.GreaterOrEqual(solver.Grid.VelocityY[5, 5], yValue - yValue * unitError);
            Assert.GreaterOrEqual(solver.Grid.VelocityY[6, 5], yValue - yValue * unitError);
            Assert.GreaterOrEqual(solver.Grid.VelocityY[5, 4], yValue - yValue * unitError);
            Assert.GreaterOrEqual(solver.Grid.VelocityY[6, 4], yValue - yValue * unitError);
        }
        else
        {
            Assert.LessOrEqual(solver.Grid.VelocityY[5, 5], yValue - yValue * unitError);
            Assert.LessOrEqual(solver.Grid.VelocityY[6, 5], yValue - yValue * unitError);
            Assert.LessOrEqual(solver.Grid.VelocityY[5, 4], yValue - yValue * unitError);
            Assert.LessOrEqual(solver.Grid.VelocityY[6, 4], yValue - yValue * unitError);
        }
    }

    [TestCaseSource(nameof(VelocityTestParameters))]
    public void VelocityAdditionTestWithRectangle((int gridSize, float timeStep, MatterState matterState, float viscosity, int stepCount, float gravity, float xValue, float yValue) param_)
    {
        int gridSize = param_.gridSize;
        float timeStep = param_.timeStep;
        MatterState matterState = param_.matterState;
        float viscosity = param_.viscosity;
        int stepCount = param_.stepCount;
        float gravity = param_.gravity;
        float xValue = param_.xValue;
        float yValue = param_.yValue;

        PDESolver solver = new PDESolver(gridSize, timeStep, matterState, viscosity, stepCount, gravity, new WallType[gridSize + 2, gridSize + 2]);

        (int, int)[] squarePosition = { (5, 5), (6, 5), (7, 5), (5, 4), (6, 4), (7, 4) };
        solver.UpdateVelocity(xValue, yValue, squarePosition);
        solver.UpdateDensity();

        if (xValue - xValue * unitError > 0)
        {
            Assert.GreaterOrEqual(solver.Grid.VelocityX[5, 5], xValue - xValue * unitError);
            Assert.GreaterOrEqual(solver.Grid.VelocityX[6, 5], xValue - xValue * unitError);
            Assert.GreaterOrEqual(solver.Grid.VelocityX[7, 5], xValue - xValue * unitError);
            Assert.GreaterOrEqual(solver.Grid.VelocityX[5, 4], xValue - xValue * unitError);
            Assert.GreaterOrEqual(solver.Grid.VelocityX[6, 4], xValue - xValue * unitError);
            Assert.GreaterOrEqual(solver.Grid.VelocityX[7, 4], xValue - xValue * unitError);
        }
        else
        {
            Assert.LessOrEqual(solver.Grid.VelocityX[5, 5], xValue - xValue * unitError);
            Assert.LessOrEqual(solver.Grid.VelocityX[6, 5], xValue - xValue * unitError);
            Assert.LessOrEqual(solver.Grid.VelocityX[7, 5], xValue - xValue * unitError);
            Assert.LessOrEqual(solver.Grid.VelocityX[5, 4], xValue - xValue * unitError);
            Assert.LessOrEqual(solver.Grid.VelocityX[6, 4], xValue - xValue * unitError);
            Assert.LessOrEqual(solver.Grid.VelocityX[7, 4], xValue - xValue * unitError);
        }

        if (yValue - yValue * unitError > 0)
        {
            Assert.GreaterOrEqual(solver.Grid.VelocityY[5, 5], yValue - yValue * unitError);
            Assert.GreaterOrEqual(solver.Grid.VelocityY[6, 5], yValue - yValue * unitError);
            Assert.GreaterOrEqual(solver.Grid.VelocityY[7, 5], yValue - yValue * unitError);
            Assert.GreaterOrEqual(solver.Grid.VelocityY[5, 4], yValue - yValue * unitError);
            Assert.GreaterOrEqual(solver.Grid.VelocityY[6, 4], yValue - yValue * unitError);
            Assert.GreaterOrEqual(solver.Grid.VelocityY[7, 4], yValue - yValue * unitError);
        }
        else
        {
            Assert.LessOrEqual(solver.Grid.VelocityY[5, 5], yValue - yValue * unitError);
            Assert.LessOrEqual(solver.Grid.VelocityY[6, 5], yValue - yValue * unitError);
            Assert.LessOrEqual(solver.Grid.VelocityY[7, 5], yValue - yValue * unitError);
            Assert.LessOrEqual(solver.Grid.VelocityY[5, 4], yValue - yValue * unitError);
            Assert.LessOrEqual(solver.Grid.VelocityY[6, 4], yValue - yValue * unitError);
            Assert.LessOrEqual(solver.Grid.VelocityY[7, 4], yValue - yValue * unitError);
        }
    }

    #endregion

}
