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

    public class Attractor_V
    {
        private VertexArray m_vertices;
        private Random getRandom;

        private float mouse_x = Single.NaN;
        private float mouse_y = Single.NaN;
        private float cam_angle_a = 2.41f;
        private float cam_angle_b = 6.15f;
        private float cam_distance = 4.158f;
        private float f_length = 0.1f;
        private float f_ratio = 0.3f;

        private float[,] particle_array;
        private float[,] trail_record;
        private attractor_constuctor attractorConstuctor;

        private Vector3 cam_x_axis = new Vector3(0, 1, 0);
        private Vector3 cam_y_axis = new Vector3(0, 0, 1);
        private Vector3 cam_z_axis = new Vector3(1, 0, 0);
        private Vector3 cam_target_pos = new Vector3(7.53f, -11.79f, 29.9f);

        private int record_size = 10;
        private int record_iter = 20;
        private int record_step = 1000;
        private int record_flag;
        private List<int> record_id;

        public Attractor_V(float[,] array, attractor_constuctor attractorConstuctor)
        {
            update_camera_axis();
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

            this.attractorConstuctor = attractorConstuctor;

            particle_array = array;
        }

        private void update_camera_axis()
        {
            cam_x_axis = new Vector3((float) -Math.Sin(cam_angle_a), (float) Math.Cos(cam_angle_a), 0);
            cam_y_axis = new Vector3((float) (-Math.Cos(cam_angle_a) * Math.Sin(cam_angle_b)),
                (float) (-Math.Sin(cam_angle_a) * Math.Sin(cam_angle_b)), (float) Math.Cos(cam_angle_b));
            cam_z_axis = new Vector3((float) (Math.Cos(cam_angle_a) * Math.Cos(cam_angle_b)),
                (float) (Math.Sin(cam_angle_a) * Math.Cos(cam_angle_b)), (float) Math.Sin(cam_angle_b));
        }

        private Vector2 View_from_Camera(Vector3 world_coor, ref bool flag)
        {
            flag = false;
            Vector3 coor = world_coor + Vector3.Multiply(cam_distance, cam_z_axis) - cam_target_pos;
            float proj_ratio = f_length / Vector3.Dot(coor, cam_z_axis);
            Vector2 cam_coor = new Vector2();
            if (proj_ratio > 0)
            {
                flag = true;
                cam_coor.X = proj_ratio * Vector3.Dot(coor, cam_x_axis) * 3000f;
                cam_coor.Y = proj_ratio * Vector3.Dot(coor, cam_y_axis) * 3000f;
            }

            return cam_coor;
        }
        
        private VertexArray iter_for_cam_view(float[,] world_array, PrimitiveType vertex_type,
            Func<float, float, int, Vertex> getVertex)
        {
            bool flag = false;
            m_vertices = new VertexArray(vertex_type, (uint) particle_array.GetLength(0));
            for (int ii = 0; ii < world_array.GetLength(0); ii += 1)
            {
                Vector3 world_coor = new Vector3(world_array[ii, 0], world_array[ii, 1], world_array[ii, 2]);
                Vector2 cam_coor = View_from_Camera(world_coor, ref flag);
                if (flag)
                {
                    m_vertices[(uint) ii] = getVertex(cam_coor.X, cam_coor.Y, ii);
                }
            }

            return m_vertices;
        }

        private List<VertexArray> GetTrail()
        {
            bool flag = false;
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
                    
                    Vector3 world_coor = new Vector3(trail_record[ii * record_step + kk, 0], trail_record[ii * record_step + kk, 1], trail_record[ii * record_step + kk, 2]);
                    Vector2 cam_coor = View_from_Camera(world_coor, ref flag);
                    if (flag)
                    {
                        Color color = Color.Cyan;
                        color.A = (byte) (255 * (Math.Exp(-(float) (kk) / (float) (record_step / 2f))));
                        m_vertices.Append(new Vertex(new Vector2f(x: cam_coor.X, y: cam_coor.Y), color: color));
                    }
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
                    if (Mouse.IsButtonPressed(Mouse.Button.Left))
                    {
                        if (!float.IsNaN(mouse_x) && !float.IsNaN(mouse_y))
                        {
                            if (Keyboard.IsKeyPressed(Keyboard.Key.LControl))
                            {
                                if (!Keyboard.IsKeyPressed(Keyboard.Key.Y))
                                {
                                    cam_target_pos += cam_x_axis * (e.X - mouse_x) / 20f;
                                }

                                if (!Keyboard.IsKeyPressed(Keyboard.Key.X))
                                {
                                    cam_target_pos += cam_y_axis * (e.Y - mouse_y) / 20f;
                                }
                                Console.WriteLine(cam_target_pos.ToString());
                                Console.WriteLine("##############");
                            }
                            else
                            {
                                if (!Keyboard.IsKeyPressed(Keyboard.Key.Y))
                                {
                                    cam_angle_a -= (e.X - mouse_x) / 300f;
                                    if (cam_angle_a < 0) cam_angle_a += (float) (2 * Math.PI);
                                }

                                if (!Keyboard.IsKeyPressed(Keyboard.Key.X))
                                {
                                    cam_angle_b -= (e.Y - mouse_y) / 300f;
                                    if (cam_angle_b < 0) cam_angle_b += (float) (2 * Math.PI);
                                }
                                
                                Console.WriteLine(cam_angle_a);
                                Console.WriteLine(cam_angle_b);
                                Console.WriteLine("---------------------");
                            }
                        }

                        mouse_x = e.X;
                        mouse_y = e.Y;
                        update_camera_axis();
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
                    cam_distance -= e.Delta * 0.5f;
                    cam_distance = Math.Max(cam_distance, 0);
                    // f_length = cam_distance * f_ratio;
                    
                    Console.WriteLine(cam_distance);
                };

            while (window.IsOpen)
            {
                Time elapsed = clock.Restart();
                update(elapsed);
                // cam_angle_b += 0.025f;

                window.DispatchEvents();
                window.Clear();
                DateTime time = DateTime.Now;
                VertexArray m_vertices = iter_for_cam_view(particle_array, PrimitiveType.Points, particleVertex);
                // VertexArray axis = View_from_Camera(axis_coordinate, PrimitiveType.Lines, axisVertex);
                List<VertexArray> trail_vertices = GetTrail();
                foreach (var trail in trail_vertices)
                {
                    window.Draw(trail);
                }
                window.Draw(m_vertices);
                // window.Draw(axis);
                window.Display();
            }
        }
    }
}