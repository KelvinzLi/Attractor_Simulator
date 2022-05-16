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

    public class Attractor_SIMD
    {
        private VertexArray m_vertices;
        private Random getRandom;

        private float mouse_x = Single.NaN;
        private float mouse_y = Single.NaN;
        private float cam_angle_x;
        private float cam_angle_y;
        private float cam_distance = 10;
        private float f_length = 0.1f;
        private float f_ratio = 0.5f;

        private float[,] particle_array;
        private attractor_constuctor attractorConstuctor;

        private Matrix4x4 base_transform;

        private int record_size = 20;
        private int record_iter = 20;
        private int record_step = 400;
        private int record_flag;
        private List<int> record_id;
        private float[,] trail_record;

        float[,] axis_coordinate =
        {
            {5, 0, 0, 1},
            {-5, 0, 0, 1},
            {0, 5, 0, 1},
            {0, -5, 0, 1},
            {0, 0, 5, 1},
            {0, 0, -5, 1}
        };

        public Attractor_SIMD(float[,] array, attractor_constuctor attractorConstuctor)
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

            trail_record = new float[record_size * record_step, 3];

            for (int ii = 0; ii < record_size * record_step; ii += 1)
            {
                trail_record[ii, 0] = Single.NaN;
                trail_record[ii, 1] = Single.NaN;
                trail_record[ii, 2] = Single.NaN;
            }

            base_transform = new Matrix4x4(
                0, 0, -1, 0,
                0, 1, 0, 0,
                1, 0, 0, cam_distance,
                0, 0, 0, 1
            );

            this.attractorConstuctor = attractorConstuctor;

            particle_array = array;
        }

        private Matrix4x4 getTransform()
        {
            float rad_x = cam_angle_x * (float) Math.PI / 180f;
            float rad_y = cam_angle_y * (float) Math.PI / 180f;
            Matrix4x4 rot_matrix_x = new Matrix4x4
            (
                (float) Math.Cos(rad_x), -1 * (float) Math.Sin(rad_x), 0, 0,
                (float) Math.Sin(rad_x), (float) Math.Cos(rad_x), 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1
            );

            Matrix4x4 rot_matrix_y = new Matrix4x4
            (
                1, 0, 0, 0,
                0, (float) Math.Cos(rad_y), -1 * (float) Math.Sin(rad_y), 0,
                0, (float) Math.Sin(rad_y), (float) Math.Cos(rad_y), 0,
                0, 0, 0, 1
            );
            
            Matrix4x4 rot_matrix = Matrix4x4.Multiply(rot_matrix_x, rot_matrix_y);
            
            return Matrix4x4.Multiply(base_transform, rot_matrix);
        }

        private VertexArray View_from_Camera(float[,] world_array, PrimitiveType vertex_type,
            Func<float, float, int, Vertex> getVertex)
        {
            float[,] cam_coordinates =
                MatrixDotProduct(world_array, Matrix4x4.Transpose(getTransform()));
            m_vertices = new VertexArray(vertex_type, (uint) particle_array.GetLength(0));
            for (int ii = 0; ii < world_array.GetLength(0); ii += 1)
            {
                {
                    float cam_X = cam_coordinates[ii, 0] * f_length / Math.Abs(cam_coordinates[ii, 2]) * 200;
                    float cam_Y = cam_coordinates[ii, 1] * f_length / Math.Abs(cam_coordinates[ii, 2]) * 200;
                    m_vertices[(uint) ii] = getVertex(cam_X, cam_Y, ii);
                }
            }

            return m_vertices;
        }

        private List<VertexArray> GetTrail()
        {
            float[,] cam_coordinates =
                MatrixDotProduct(trail_record, Matrix4x4.Transpose(getTransform()));
            List<VertexArray> trail_vertices = new List<VertexArray>();
            for (int ii = 0; ii < record_size; ii += 1)
            {
                m_vertices = new VertexArray(PrimitiveType.LineStrip);
                for (int kk = 0; kk < record_step; kk += 1)
                {
                    if (float.IsNaN(trail_record[ii * record_step + kk, 0]))
                    {
                        break;
                    }

                    float cam_X = cam_coordinates[ii * record_step + kk, 0] * f_length /
                                  Math.Abs(cam_coordinates[ii * record_step + kk, 2]);
                    float cam_Y = cam_coordinates[ii * record_step + kk, 1] * f_length /
                                  Math.Abs(cam_coordinates[ii * record_step + kk, 2]);
                    Color color = Color.Cyan;
                    color.A = (byte) (255 * (Math.Exp(-(float) (kk) / (float) (record_step / 2f))));
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
            return new Vertex(new Vector2f(x: x, y: y), color: color_list[(int) (ii / 2)]);
        }

        private float[,] MatrixDotProduct(float[,] array, Matrix4x4 tranform_matrix)
        {
            float[,] output = new float[array.GetLength(0), 3];
            float[] array_result = new float[4];

            for (int ii = 0; ii < array.GetLength(0); ii += 1)
            {
                Vector4 result = Vector4.Transform(new Vector4(array[ii, 0], array[ii, 1], array[ii, 2], 1), tranform_matrix);
                result.CopyTo(array_result);
                for (int jj = 0; jj < 3; jj += 1)
                {
                    output[ii, jj] = array_result[jj];
                }
            }

            return output;
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
                    elapsed.AsSeconds() / 150;
                float y_step =
                    attractorConstuctor.Y_slope(particle_array[ii, 0], particle_array[ii, 1], particle_array[ii, 2]) *
                    elapsed.AsSeconds() / 150;
                float z_step =
                    attractorConstuctor.Z_slope(particle_array[ii, 0], particle_array[ii, 1], particle_array[ii, 2]) *
                    elapsed.AsSeconds() / 150;

                if (float.IsNaN(x_step))
                {
                    Console.WriteLine(x_step);
                }

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
                    cam_distance -= e.Delta * 0.05f;
                    cam_distance = Math.Max(cam_distance, 0);
                    // f_length = cam_distance * f_ratio;
                    
                    Console.WriteLine(cam_distance);
                    
                    base_transform = new Matrix4x4(
                        0, 0, -1, 0,
                        0, 1, 0, 0,
                        1, 0, 0, cam_distance,
                        0, 0, 0, 1
                    );
                };

            while (window.IsOpen)
            {
                Time elapsed = clock.Restart();
                update(elapsed);

                window.DispatchEvents();
                window.Clear();
                DateTime time = DateTime.Now;
                VertexArray m_vertices = View_from_Camera(particle_array, PrimitiveType.Points, particleVertex);
                VertexArray axis = View_from_Camera(axis_coordinate, PrimitiveType.Lines, axisVertex);
                // List<VertexArray> trail_vertices = GetTrail();
                // foreach (var trail in trail_vertices)
                // {
                //     window.Draw(trail);
                // }
                window.Draw(m_vertices);
                // window.Draw(axis);
                window.Display();
            }
        }
    }
}