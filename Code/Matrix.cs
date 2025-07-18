using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Adjustment
{
    class Matrix
    {
        public static double[,] Inverse(double[,] matrix)
        {
            int n = matrix.GetLength(0);

            //初始化变换矩阵
            double[,] temp = new double[n, 2 * n];
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    temp[i, j] = matrix[i, j];
                    if (i == j)
                    {
                        temp[i, n + j] = 1.0;
                    }
                    else
                    {
                        temp[i, n + j] = 0.0;
                    }
                }
            }

            //按列循环进行
            for (int i = 0; i < n; i++)
            {
                //第i，i项等于0的情况
                if (temp[i, i] == 0)
                {
                    for (int j = i; j < n; j++)
                    {
                        if (temp[j, i] != 0)
                        {
                            double m = temp[j, i];
                            for (int k = 0; k < temp.GetLength(1); k++)
                            {
                                temp[i, k] += temp[j, k];
                                temp[i, k] /= m;
                            }
                            break;
                        }
                    }
                }
                //第i，i项不等于1的情况
                else if (temp[i, i] != 1)
                {
                    double m = temp[i, i];
                    for (int k = 0; k < temp.GetLength(1); k++)
                    {
                        temp[i, k] /= m;
                    }
                }
                //第i列除第i，i项全化为0
                for (int j = 0; j < n; j++)
                {
                    if (temp[j, i] != 0 && j != i)
                    {
                        double m = temp[j, i];
                        for (int k = 0; k < temp.GetLength(1); k++)
                        {
                            temp[j, k] -= temp[i, k] * m;
                        }
                    }
                }
            }
            //结果赋值
            double[,] result = new double[n, n];
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    result[i, j] = temp[i, j + n];
                }
            }
            return result;
        }

        public static double[,] Transpose(double[,] matrix)
        {
            double[,] result = new double[matrix.GetLength(1), matrix.GetLength(0)];
            for (int i = 0; i < result.GetLength(0); i++)
            {
                for (int j = 0; j < result.GetLength(1); j++)
                {
                    result[i, j] = matrix[j, i];
                }
            }
            return result;
        }

        public static double[,] Multiply(double[,] matrix1, double[,] matrix2)
        {
            if (matrix1.GetLength(1) != matrix2.GetLength(0))
            {
                MessageBox.Show("无法相乘");
                return null;
            }
            double[,] result = new double[matrix1.GetLength(0), matrix2.GetLength(1)];
            for (int i = 0; i < result.GetLength(0); i++)
            {
                for (int j = 0; j < result.GetLength(1); j++)
                {
                    for (int k = 0; k < matrix2.GetLength(0); k++)
                    {
                        result[i, j] += matrix1[i, k] * matrix2[k, j];
                    }
                }
            }
            return result;
        }


        public static double[,] Sub(double[,] matrix1, double[,] matrix2)
        {
            if (matrix1.GetLength(0) != matrix2.GetLength(0) && matrix1.GetLength(1) != matrix2.GetLength(1))
            {
                MessageBox.Show("无法相加");
                return null;
            }
            double[,] result = new double[matrix1.GetLength(0), matrix1.GetLength(1)];
            for (int i = 0; i < matrix1.GetLength(0); i++)
            {
                for (int j = 0; j < matrix1.GetLength(1); j++)
                {
                    result[i, j] = matrix1[i, j] - matrix2[i, j];
                }
            }
            return result;
        }
    }
}
