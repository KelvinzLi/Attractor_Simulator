using System;
using SFML.Graphics;
using SFML.Audio;
using System;
using SFML.Window;
using SFML.Graphics;
using SFML.System;
using global::System.Collections.Generic;
using global::System.Numerics;

namespace SFML
{
    using global::System.Linq;
    using global::System.Security.Principal;
    using global::System.Threading.Tasks;
    using MathNet.Numerics.LinearAlgebra;
    using MathNet.Numerics.LinearAlgebra.Single;

    public class Attractor
    {
        private VertexArray m_vertices;
        private Random getRandom;

        private float mouse_x = Single.NaN;
        private float mouse_y = Single.NaN;
        private float cam_angle_x;
        private float cam_angle_y;
        private float cam_distance = 100;
        private float f_length;
        private float f_ratio = 0.2f;

        private float[,] particle_array;
        private attractor_constuctor attractorConstuctor;

        private float[,] base_transform;

        private int record_size = 20;
        private int record_iter = 20;
        private int record_step = 250;
        private int record_flag;
        private List<int> record_id;
        private float[,] trail_record;
        
        float[,] axis_coordinate =
        {
            {60, 0, 0, 1},
            {-60, 0, 0, 1},
            {0, 60, 0, 1},
            {0, -60, 0, 1},
            {0, 0, 60, 1},
            {0, 0, -60, 1}
        };

        public Attractor(float[,] array, attractor_constuctor attractorConstuctor)
        {
            record_flag = 0;
            
            Random rand = new Random();
            record_id = new List<int>();

            while (record_id.Count < record_size)
            {
                int rand_num = rand.Next(0, array.GetLength(0) - 1);
                if (!record_id.Any(x => x == rand_num))
                {
                    record_id.Add(rand_num);
                }
            }

            trail_record = new float[record_size * record_step, 4];

            for (int ii = 0; ii < record_size * record_step; ii += 1)
            {
                trail_record[ii, 0] = Single.NaN;
                trail_record[ii, 1] = Single.NaN;
                trail_record[ii, 2] = Single.NaN;
                trail_record[ii, 3] = 1;
            }
            
            base_transform = new float[,]
            {
                {0, 0, -1, 0},
                {0, 1, 0, 0},
                {1, 0, 0, cam_distance},
                {0, 0, 0, 1}
            };

            this.attractorConstuctor = attractorConstuctor;

            particle_array = array;
        }

        private float[,] getTransform()
        {
            float rad_x = cam_angle_x * (float)Math.PI / 180f;
            float rad_y = cam_angle_y * (float)Math.PI / 180f;
            float[,] rot_matrix_x = 
            {
                {(float)Math.Cos(rad_x), -1 * (float)Math.Sin(rad_x), 0, 0},
                {(float)Math.Sin(rad_x), (float)Math.Cos(rad_x), 0, 0},
                {0, 0, 1, 0},
                {0, 0, 0, 1}
            };
            
            float[,] rot_matrix_y = 
            {
                {1, 0, 0, 0},
                {0, (float)Math.Cos(rad_y), -1 * (float)Math.Sin(rad_y), 0},
                {0, (float)Math.Sin(rad_y), (float)Math.Cos(rad_y), 0},
                {0, 0, 0, 1}
            };
            
            float[,] rot_matrix = MatrixCalculation.dotProduct(rot_matrix_x, rot_matrix_y);

            return MatrixCalculation.dotProduct(base_transform, rot_matrix);
        }

        private VertexArray View_from_Camera(float[,] world_array, PrimitiveType vertex_type, Func<float , float, int, Vertex> getVertex)
        {
            float[,] cam_coordinates =
                MatrixCalculation.dotProduct(world_array, MatrixCalculation.Transpose(getTransform()));
            m_vertices = new VertexArray(vertex_type, (uint)particle_array.GetLength(0));
            for (int ii = 0; ii < world_array.GetLength(0); ii += 1)
            {
                float cam_X = cam_coordinates[ii, 0] * f_length / Math.Abs(cam_coordinates[ii, 2]);
                float cam_Y = cam_coordinates[ii, 1] * f_length / Math.Abs(cam_coordinates[ii, 2]);
                m_vertices[(uint) ii] = getVertex(cam_X, cam_Y, ii);
            }

            return m_vertices;
        }
        
        private List<VertexArray> GetTrail()
        {
            float[,] cam_coordinates =
                MatrixCalculation.dotProduct(trail_record, MatrixCalculation.Transpose(getTransform()));
            List<VertexArray> trail_vertices = new List<VertexArray>();
            for (int ii = 0; ii < record_size; ii += 1)
            {
                m_vertices = new VertexArray(PrimitiveType.LineStrip);
                for (int kk = 0; kk < record_step; kk += 1)
                {
                    if (float.IsNaN(trail_record[ii * record_step + kk, 0])) {break;}
                    float cam_X = cam_coordinates[ii * record_step + kk, 0] * f_length / Math.Abs(cam_coordinates[ii * record_step + kk, 2]);
                    float cam_Y = cam_coordinates[ii * record_step + kk, 1] * f_length / Math.Abs(cam_coordinates[ii * record_step + kk, 2]);
                    Color color = Color.Cyan;
                    color.A = (byte) (255 * (Math.Exp(-(float)(kk) / (float)(record_step / 2f))));
                    m_vertices.Append(new Vertex(new Vector2f(x: cam_X, y: cam_Y), color: color));
                }
                trail_vertices.Add(m_vertices);
            }

            return trail_vertices;
        }

        private Vertex particleVertex(float x, float y, int ii)
        {
            return new Vertex(new Vector2f(x: x, y: y));
        }

        private Vertex axisVertex(float x, float y, int ii)
        {
            Color[] color_list = {Color.Blue, Color.Green, Color.Red,};
            return new Vertex(new Vector2f(x: x, y: y), color: color_list[(int)(ii/2)]);
        }

        private void update(Time elapsed)
        {
            record_flag += 1;
            bool DoRecord = false;
            if (record_flag == record_iter)
            {
                record_flag = 0;
                DoRecord = true;
            }
            for (int ii = 0; ii < particle_array.GetLength(0); ii += 1)
            {
                float x_step =
                    attractorConstuctor.X_slope(particle_array[ii, 0], particle_array[ii, 1], particle_array[ii, 2]) *
                    elapsed.AsSeconds()/200;
                float y_step =
                    attractorConstuctor.Y_slope(particle_array[ii, 0], particle_array[ii, 1], particle_array[ii, 2]) *
                    elapsed.AsSeconds()/200;
                float z_step =
                    attractorConstuctor.Z_slope(particle_array[ii, 0], particle_array[ii, 1], particle_array[ii, 2]) *
                    elapsed.AsSeconds()/200;

                particle_array[ii, 0] += x_step;
                particle_array[ii, 1] += y_step;
                particle_array[ii, 2] += z_step;

                if (record_id.Any(x => x == ii) && DoRecord)
                {
                    int id = record_id.IndexOf(ii);
                    for (int kk = record_step - 1; kk >= 0; kk -= 1)
                    {
                        if (kk != 0)
                        {
                            for (int num = 0; num < 3; num += 1)
                            {
                                trail_record[kk + id * record_step, num] = trail_record[kk + id * record_step - 1, num];
                            }
                        }
                        else
                        {
                            for (int num = 0; num < 3; num += 1)
                            {
                                trail_record[kk + id * record_step, num] = particle_array[ii, num];
                            }
                        }
                    }
                }
            }
        }

        public void Show()
        {
            Clock clock = new Clock();

            VideoMode mode = new VideoMode(2048, 1024);
            RenderWindow window = new RenderWindow(mode, "Particle System");
            View view = new View(new Vector2f(0, 0), new Vector2f(window.Size.X, window.Size.Y));
            window.SetView(view);

            window.Closed += (obj, e) => { window.Close(); };
            window.KeyPressed +=
                (sender, e) =>
                {
                    RenderWindow window = (RenderWindow) sender;
                    if (e.Code == Keyboard.Key.Escape)
                    {
                        window.Close();
                    }
                };
            window.Resized +=
                (sender, e) =>
                {
                    view.Size = new Vector2f((int) window.Size.X, (int) window.Size.Y);
                    window.SetView(view);
                };
            window.MouseMoved +=
                (sender, e) =>
                {
                    // Console.WriteLine(e.X);
                    if (Mouse.IsButtonPressed(Mouse.Button.Left))
                    {
                        if (!float.IsNaN(mouse_x) && !float.IsNaN(mouse_y))
                        {
                            if (!Keyboard.IsKeyPressed(Keyboard.Key.Y))
                            {
                                cam_angle_y -= (int) ((e.X - mouse_x) / 5);
                            }
                            if (!Keyboard.IsKeyPressed(Keyboard.Key.X))
                            {
                                cam_angle_x -= (int) ((e.Y - mouse_y) / 5);
                            }
                        }
                        mouse_x = e.X;
                        mouse_y = e.Y;
                    }
                    else
                    {
                        mouse_x = Single.NaN;
                        mouse_y = Single.NaN;
                    }
                };
            window.MouseWheelScrolled +=
                (sender, e) =>
                {
                    cam_distance += e.Delta * 5;
                    f_length = cam_distance * f_ratio;
                };

            while (window.IsOpen)
            {
                DateTime start = DateTime.Now;
                Time elapsed = clock.Restart();
                update(elapsed);

                window.DispatchEvents();
                window.Clear();
                DateTime time = DateTime.Now;
                VertexArray m_vertices = View_from_Camera(particle_array, PrimitiveType.Points, particleVertex);
                VertexArray axis = View_from_Camera(axis_coordinate, PrimitiveType.Lines, axisVertex);
                List<VertexArray> trail_vertices = GetTrail();
                foreach (var trail in trail_vertices)
                {
                    window.Draw(trail);
                }
                window.Draw(m_vertices);
                window.Draw(axis);
                window.Display();
                
                Console.WriteLine(DateTime.Now.Subtract(start).TotalMilliseconds);
            }
        }
    }

    public interface attractor_constuctor
    {
        float X_slope(float x, float y, float z);
        float Y_slope(float x, float y, float z);
        float Z_slope(float x, float y, float z);
    }

    public class MatrixCalculation
    {
        public static float[,] dotProduct(float[,] matrix1, float[,] matrix2)
        {
            float[,] matrix = new float[matrix1.GetLength(0), matrix2.GetLength(1)];
            if (matrix1.GetLength(1) == matrix2.GetLength(0))
            {
                Parallel.For(0, matrix1.GetLength(0), i =>
                {
                    for (int j = 0; j < matrix2.GetLength(1); j += 1)
                    {
                        float[] row = GetRow(matrix1, i);
                        float[] column = GetColumn(matrix2, j);
                        float x = 0f;
                        for (int k = 0; k < matrix1.GetLength(1); k += 1)
                        {
                            x += row[k] * column[k];
                        }

                        matrix[i, j] = x;
                    }
                });
            }
            else
            {
                Console.WriteLine(matrix1.GetLength(0));
                Console.WriteLine(matrix1.GetLength(1));
                Console.WriteLine(matrix2.GetLength(0));
                Console.WriteLine(matrix2.GetLength(1));
                Console.WriteLine("Matrix Sizes Don't Match");
            }

            return matrix;
        }

        public static float[,] Transpose(float[,] matrix)
        {
            int height = matrix.GetLength(0);
            int width = matrix.GetLength(1);
            float[,] matrix1 = new float[width, height];

            for (int ii = 0; ii < height; ii += 1)
            {
                for (int jj = 0; jj < width; jj += 1)
                {
                    matrix1[jj, ii] = matrix[ii, jj];
                }
            }

            return matrix1;
        }

        public static float[] GetColumn(float[,] matrix, int columnNumber)
        {
            return Enumerable.Range(0, matrix.GetLength(0))
                .Select(x => matrix[x, columnNumber])
                .ToArray();
        }

        public static float[] GetRow(float[,] matrix, int rowNumber)
        {
            return Enumerable.Range(0, matrix.GetLength(1))
                .Select(x => matrix[rowNumber, x])
                .ToArray();
        }
    }
}