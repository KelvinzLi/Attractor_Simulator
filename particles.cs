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
    using global::System.Runtime.Loader;

    class particles
    {
        private List<Particle> m_particles;
        private VertexArray m_vertices;
        private Time m_lifetime;
        private Vector2f m_emitter;
        private Random getRandom;
        private Vector2f acceleration;

        private Transformable transformable;
        private Drawable drawable;

        public particles (int count)
        {
            m_particles = new List<Particle>(new Particle[count]);
            m_emitter = new Vector2f(0f, 0f);
            m_vertices = new VertexArray(PrimitiveType.Points, Convert.ToUInt32(count));
            m_lifetime = Time.FromSeconds(10f);

            acceleration = new Vector2f(x: 0, y: 50);
        }
        
        struct Particle
        {
            public Vector2f velocity;
            public Time lifetime;
        }
        
        private void setEmitter(Vector2f position)
        {
            m_emitter = position;
        }

        private void resetParticle(int index)
        {
            getRandom = new Random();
            Double angle = getRandom.NextDouble() * Math.PI * 2;
            Double velocity = getRandom.NextDouble() * 50f + 50f;

            Particle m_particle = new Particle();
            m_particle.velocity = new Vector2f((float)(Math.Cos(angle) * velocity), (float)(Math.Sin(angle) * velocity));
            m_particle.lifetime = Time.FromMilliseconds((int)(getRandom.NextDouble() * 3000 + 7000));
            m_particles[index] = m_particle;

            Vertex m_vertex = m_vertices[Convert.ToUInt16(index)];
            m_vertex.Position = m_emitter;
            m_vertices[Convert.ToUInt16(index)] = m_vertex;
        }

        private void update(Time elapsed)
        {
            for (int ii = 0; ii < m_particles.Count; ii += 1)
            {
                Particle p = m_particles[ii];

                p.lifetime -= elapsed;

                if (p.lifetime <= Time.Zero)
                {
                    resetParticle(ii);
                }
                else
                {
                    p.velocity += acceleration * elapsed.AsSeconds();
                    m_particles[ii] = p;

                    Vertex m_vertex = m_vertices[Convert.ToUInt16(ii)];

                    m_vertex.Position += p.velocity * elapsed.AsSeconds();

                    float ratio = p.lifetime.AsSeconds() / m_lifetime.AsSeconds();
                    m_vertex.Color.A = (byte) (255 * ratio);

                    m_vertices[Convert.ToUInt16(ii)] = m_vertex;
                }
            }
        }

        public VertexArray particleVertice
        {
            get { return m_vertices; }
        }
        
        public void Show()
        {
            Clock clock = new Clock();
            
            VideoMode mode = new VideoMode(512, 256);
            RenderWindow window = new RenderWindow(mode, "Particle System");

            window.Closed += (obj, e) => { window.Close(); };
            window.KeyPressed +=
                (sender, e) =>
                {
                    RenderWindow window = (RenderWindow)sender;
                    if (e.Code == Keyboard.Key.Escape)
                    {
                        window.Close();
                    }
                };
            window.Resized +=
                (sender, e) =>
                {
                    View view = window.DefaultView;
                    view.Size = new Vector2f((int) window.Size.X, (int) window.Size.Y);
                    window.SetView(view);
                };

            while (window.IsOpen)
            {
                Vector2i mouse = Mouse.GetPosition(window);
                Console.WriteLine("x:" + mouse.X);
                Console.WriteLine("y:" + mouse.Y);
                setEmitter(window.MapPixelToCoords(mouse));

                Time elapsed = clock.Restart();
                update(elapsed);
                
                window.DispatchEvents();
                window.Clear();
                window.Draw(m_vertices);
                window.Display();
            }
        }
    }
}