using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuralNetwork1
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Accord.Neuro.Learning;

    namespace NeuralNetwork1
    {
        public class StudentNetwork : BaseNetwork
        {
            private int[] structure;
            private double[] biases;
            private double[][][] weights;

            private double[][] neurons;
            private double[][] errors;

            public Stopwatch stopWatch = new Stopwatch();
            private Random random = new Random();

            const double learningRate = 0.1;

            public StudentNetwork(int[] structure)
            {
                this.structure = structure;
                Init();
            }

            private void Init()
            {
                biases = new double[structure.Length - 1];
                weights = new double[structure.Length - 1][][];
                for (int i = 0; i < structure.Length - 1; i++)
                {
                    biases[i] = random.NextDouble() * 2 - 1;
                    weights[i] = new double[structure[i]][];
                    for (int j = 0; j < structure[i]; j++)
                    {
                        weights[i][j] = new double[structure[i + 1]];
                        for (int k = 0; k < structure[i + 1]; k++)
                        {
                            weights[i][j][k] = random.NextDouble() * 2 - 1;
                        }
                    }
                }

                errors = new double[structure.Length][];
                neurons = new double[structure.Length][];
                for (int i = 0; i < structure.Length; i++)
                {
                    errors[i] = new double[structure[i]];
                    neurons[i] = new double[structure[i]];
                }
            }

            private double Sigmoid(double x)
            {
                return 1.0d / (1.0d + Math.Exp(-x));
            }

            private double SigmoidDerivative(double x)
            {
                return x * (1.0 - x);
            }

            public override int Train(Sample sample, double acceptableError, bool parallel)
            {
                int iters = 0;
                double error;
                do
                {
                    double[] predicted = ForwardPass(sample.input, parallel);
                    error = BackPropagation(predicted, sample.Output, parallel);
                    ++iters;
                } while (error > acceptableError);
                return iters;
            }

            public override double TrainOnDataSet(SamplesSet samplesSet, int epochsCount, double acceptableError, bool parallel)
            {
                double[][] inputs = new double[samplesSet.Count][];
                double[][] outputs = new double[samplesSet.Count][];

                for (int i = 0; i < samplesSet.Count; ++i)
                {
                    inputs[i] = samplesSet[i].input;
                    outputs[i] = samplesSet[i].Output;
                }

                int epoch_to_run = 0;

                double error = double.PositiveInfinity;

                stopWatch.Restart();

                while (epoch_to_run < epochsCount && error > acceptableError)
                {
                    epoch_to_run++;
                    error = RunEpoch(inputs, outputs, parallel);
                    OnTrainProgress((epoch_to_run * 1.0) / epochsCount, error, stopWatch.Elapsed);
                }

                OnTrainProgress(1.0, error, stopWatch.Elapsed);

                stopWatch.Stop();

                return error;
            }

            public double RunEpoch(double[][] inputs, double[][] outputs, bool parallel)
            {
                double error = 0;
                for (int i = 0; i < inputs.Length; i++)
                {
                    double[] predicted = ForwardPass(inputs[i], parallel);
                    error += BackPropagation(predicted, outputs[i], parallel);
                }
                return error;
            }

            private double[] ForwardPass(double[] input, bool parallel)
            {
                neurons[0] = input;
                if (parallel)
                {
                    for (int i = 0; i < structure.Length - 1; i++)
                    {
                        Parallel.For(0, structure[i + 1], j =>
                        {
                            double sum = biases[i];
                            for (int k = 0; k < structure[i]; k++)
                            {
                                sum += weights[i][k][j] * neurons[i][k];
                            }
                            neurons[i + 1][j] = Sigmoid(sum);
                        });
                    }
                }
                else
                {
                    for (int i = 0; i < structure.Length - 1; i++)
                    {
                        for (int j = 0; j < structure[i + 1]; j++)
                        {
                            double sum = biases[i];
                            for (int k = 0; k < structure[i]; k++)
                            {
                                sum += weights[i][k][j] * neurons[i][k];
                            }
                            neurons[i + 1][j] = Sigmoid(sum);
                        }
                    }
                }
                return neurons.Last();
            }

            private double BackPropagation(double[] predicted, double[] output, bool parallel)
            {
                if (parallel)
                {
                    for (var i = 0; i < output.Length; i++)
                    {
                        errors[errors.Length - 1][i] = SigmoidDerivative(predicted[i]) * (output[i] - predicted[i]);
                    }

                    for (int i = errors.Length - 2; i > 0; i--)
                    {
                        Parallel.For(0, errors[i].Length, j =>
                        {
                            double sum = 0;
                            var value = neurons[i][j];
                            for (int k = 0; k < errors[i + 1].Length; k++)
                            {
                                sum += errors[i + 1][k] * weights[i][j][k];
                            }
                            errors[i][j] = SigmoidDerivative(value) * sum;
                        });
                    }

                    for (int i = 0; i < structure.Length - 1; i++)
                    {
                        Parallel.For(0, structure[i], j =>
                        {
                            for (int k = 0; k < structure[i + 1]; k++)
                            {
                                weights[i][j][k] += learningRate * errors[i + 1][k] * neurons[i][j];
                            }
                        });

                        for (int j = 0; j < structure[i + 1]; j++)
                        {
                            biases[i] += learningRate * errors[i + 1][j];
                        }
                    }
                }
                else
                {
                    for (var i = 0; i < output.Length; i++)
                    {
                        errors[errors.Length - 1][i] = SigmoidDerivative(predicted[i]) * (output[i] - predicted[i]);
                    }

                    for (int i = errors.Length - 2; i > 0; i--)
                    {
                        for (int j = 0; j < errors[i].Length; j++)
                        {
                            double sum = 0;
                            var value = neurons[i][j];
                            for (int k = 0; k < errors[i + 1].Length; k++)
                            {
                                sum += errors[i + 1][k] * weights[i][j][k];
                            }
                            errors[i][j] = SigmoidDerivative(value) * sum;
                        }
                    }

                    for (int i = 0; i < weights.Length; i++)
                    {
                        for (int j = 0; j < weights[i].Length; j++)
                        {
                            for (int k = 0; k < weights[i][j].Length; k++)
                            {
                                weights[i][j][k] += learningRate * errors[i + 1][k] * neurons[i][j];
                            }
                        }

                        for (int j = 0; j < structure[i + 1]; j++)
                        {
                            biases[i] += learningRate * errors[i + 1][j];
                        }
                    }
                }

                double error = 0;
                for (int i = 0; i < output.Length; i++)
                {
                    error += Math.Pow(errors[errors.Length - 1][i], 2);
                }

                return error / output.Length;
            }

            public override double[] Compute(double[] input)
            {
                return ForwardPass(input, false);
            }
        }
    }
}
