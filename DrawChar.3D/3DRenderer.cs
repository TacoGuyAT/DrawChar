/*
* Copyright (c) 2012-2014 AssimpNet - Nicholas Woodfield
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in
* all copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
* THE SOFTWARE.
*/

using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using Assimp;
using Assimp.Configs;
using DrawChar.Core;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using ErrorCode = OpenTK.Graphics.OpenGL.ErrorCode;
using PixelFormat = OpenTK.Graphics.OpenGL.PixelFormat;
using TextureWrapMode = OpenTK.Graphics.OpenGL.TextureWrapMode;

namespace DrawChar._3D {
    public class _3DRenderer : GameWindow {
        public List<byte> Buffer = new List<byte>();

        public Action Render;
        public Camera Camera;
        Shader shader, depthShader;
        List<Model> models = new List<Model>();
        DirectionalLight sun = new DirectionalLight();
        PointLight[] pointLights = new PointLight[]
        {
            /*
            new PointLight()
            {
                position = new Vector3(0.0f, 4.0f, 0.0f)

			}
            //*/
        };


        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        public static void CheckLastError() {
            ErrorCode errorCode = GL.GetError();
            if(errorCode != ErrorCode.NoError) {
                throw new Exception(errorCode.ToString());
            }
        }


        public _3DRenderer(GameWindowSettings gws, NativeWindowSettings nws) : base(gws, nws) { }
        
        protected override void OnMouseMove(MouseMoveEventArgs e) {
            if(this.IsFocused && this.CursorState == CursorState.Grabbed) {
                    Camera.Yaw += e.DeltaX * Camera.Sensitivity;
                    Camera.Pitch -= e.DeltaY * Camera.Sensitivity; // reversed since y-coordinates range from bottom to top
            }

            base.OnMouseMove(e);
        }


        public void LoadModel(string path) {
            path = path.Replace('\\', '/');
            models.Add(new Model(path));
        }

        protected override void OnLoad() {
            Title = "RenderSpace";

            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            CheckLastError();
            CursorState = CursorState.Grabbed;

            shader = new Shader("Shaders\\shader.vs", "Shaders\\shader.fs");
            depthShader = new Shader("Shaders\\depth.vs", "Shaders\\depth.fs", "Shaders\\depth.gs");
            SetupShadowmaps(pointLights);
            SetShadowMaps(pointLights, depthShader, shader);

            Camera = new Camera(Vector3.Zero, this.Size.X / this.Size.Y);

            base.OnLoad();
            CheckLastError();
        }

        protected override void OnResize(ResizeEventArgs e) {
            Camera.AspectRatio = e.Width / (float)e.Height;
            GL.Viewport(0, 0, e.Width, e.Height);
            base.OnResize(e);
        }
        
        // TODO: Fix ASCII rendering routine

        public void UpdateABuffer() {
            // Read OpenGL buffer into a bitmap so we can iterate the pixels
            byte[] buffer = new byte[ASCIIRenderer.Width * ASCIIRenderer.Height];
            var bmp = new Bitmap(ASCIIRenderer.Width, ASCIIRenderer.Height);
            BitmapData data =
                bmp.LockBits(new Rectangle(0, 0, ASCIIRenderer.Width, ASCIIRenderer.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly,
                             System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            GL.Flush();
            GL.ReadBuffer(ReadBufferMode.Back);
            GL.ReadPixels(0, 0, ASCIIRenderer.Width, ASCIIRenderer.Height, PixelFormat.Bgr,
                          PixelType.UnsignedByte, data.Scan0);
            bmp.UnlockBits(data);
            bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
            
            for(int i = 0; i < ASCIIRenderer.Width * ASCIIRenderer.Height; i++) {
                var p = bmp.GetPixel(i % ASCIIRenderer.Width, i / ASCIIRenderer.Width);
                buffer[i] = (byte)((p.R + p.G + p.B) / 3);
            }

            Buffer = new List<byte>(buffer);
        }

        protected override void OnUpdateFrame(FrameEventArgs args) {
            var speedMultiplier = 1f;

            if(this.KeyboardState.IsKeyDown(Keys.Tab))
                speedMultiplier = 2;
            if(this.KeyboardState.IsKeyDown(Keys.LeftShift))
                speedMultiplier = 0.25f;

            if(this.KeyboardState.IsKeyDown(Keys.W))
                Camera.NextPosition += Camera.Front * Camera.Speed * speedMultiplier;
            if(this.KeyboardState.IsKeyDown(Keys.A))
                Camera.NextPosition += -Camera.Right * Camera.Speed * speedMultiplier;
            if(this.KeyboardState.IsKeyDown(Keys.S))
                Camera.NextPosition += -Camera.Front * Camera.Speed * speedMultiplier;
            if(this.KeyboardState.IsKeyDown(Keys.D))
                Camera.NextPosition += Camera.Right * Camera.Speed * speedMultiplier;

            if(this.KeyboardState.IsKeyDown(Keys.Space))
                Camera.NextPosition += new Vector3(0, 1, 0) * Camera.Speed * speedMultiplier;
            if(this.KeyboardState.IsKeyDown(Keys.LeftControl))
                Camera.NextPosition += -new Vector3(0, 1, 0) * Camera.Speed * speedMultiplier;

            if(this.KeyboardState.IsKeyPressed(Keys.Escape))
                CursorState = CursorState == CursorState.Grabbed ? CursorState.Normal : CursorState.Grabbed;

            base.OnUpdateFrame(args);
        }

        protected override void OnRenderFrame(FrameEventArgs e) {
            CheckLastError();

            Camera.Position = Vector3.Lerp(Camera.Position, Camera.NextPosition, 0.1f);

            Title = $"(Vsync: {VSync}) FPS: {1f / e.Time:0}";

            GL.Viewport(0, 0, this.Size.X, this.Size.Y);
            GL.ClearColor(Color.CornflowerBlue);
//            GL.ClearColor(Color.Black);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            shader.Use();
            Matrix4 view = Camera.GetViewMatrix();
            Matrix4 proj = Camera.GetProjectionMatrix();
            Matrix4 modelMatrix = Matrix4.CreateScale(0.1f);

            shader.SetMatrix4("viewMatrix", view);
            shader.SetMatrix4("projMatrix", proj);
            shader.SetMatrix4("modelMatrix", modelMatrix);
            shader.SetVec3("cameraPos", Camera.Position);
            sun.Set(shader, 0);

            for(int i = 0; i < pointLights.Length; i++) {
                pointLights[i].Set(shader, i);
            }

            foreach(Model m in models)
                m.Draw(shader);
            CheckLastError();

            //GL.DepthFunc(DepthFunction.Lequal);

            //GL.DepthFunc(DepthFunction.Less);
            Context.SwapBuffers();
            UpdateABuffer();
            CheckLastError();

            base.OnRenderFrame(e);
        }


        //                    //
        //   LIGHTING SETUP   //
        //                    //


        readonly int shadowWidth = 512, shadowHeight = 512;
        private int[] shadowCubemaps;
        private int[] shadowFBOs;

        private void SetupShadowmaps(PointLight[] lights) {
            shadowCubemaps = new int[lights.Length];
            shadowFBOs = new int[lights.Length];

            for(int i = 0; i < lights.Length; i++) {
                shadowCubemaps[i] = GL.GenTexture();
                GL.BindTexture(TextureTarget.TextureCubeMap, shadowCubemaps[i]);

                for(int z = 0; z < 6; z++) {
                    GL.TexImage2D(TextureTarget.TextureCubeMapPositiveX + z, 0, PixelInternalFormat.DepthComponent32f, shadowWidth, shadowHeight, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
                    CheckLastError();
                }
                GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
                GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);

                shadowFBOs[i] = GL.GenFramebuffer();
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, shadowFBOs[i]);
                GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, shadowCubemaps[i], 0);
                GL.DrawBuffer(DrawBufferMode.None);
                GL.ReadBuffer(ReadBufferMode.None);
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                CheckLastError();
            }
        }

        private void SetShadowMaps(PointLight[] lights, Shader depthShader, Shader targetShader) {
            for(int i = 0; i < lights.Length; i++) {
                GL.ClearColor(Color.DeepPink);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                GL.Viewport(0, 0, shadowWidth, shadowHeight);
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, shadowFBOs[i]);
                GL.Clear(ClearBufferMask.DepthBufferBit);

                float far_plane = 300f;

                Vector3 lightPos = pointLights[i].position;
                Matrix4 shadowProj = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(90.0f), (float)shadowWidth / shadowHeight, 0.1f, 300f);
                Matrix4[] shadowTransforms = new Matrix4[]
                {
                    Matrix4.LookAt(lightPos, lightPos + new Vector3(1.0f, 0.0f, 0.0f), new Vector3(0.0f, -1.0f, 0.0f)) * shadowProj,
                    Matrix4.LookAt(lightPos, lightPos + new Vector3(-1.0f, 0.0f, 0.0f), new Vector3(0.0f, -1.0f, 0.0f)) * shadowProj,
                    Matrix4.LookAt(lightPos, lightPos + new Vector3(0.0f, 1.0f, 0.0f), new Vector3(0.0f, 0.0f, 1.0f)) * shadowProj,
                    Matrix4.LookAt(lightPos, lightPos + new Vector3(0.0f, -1.0f, 0.0f), new Vector3(0.0f, 0.0f, -1.0f)) * shadowProj,
                    Matrix4.LookAt(lightPos, lightPos + new Vector3(0.0f, 0.0f, 1.0f), new Vector3(0.0f, -1.0f, 0.0f)) * shadowProj,
                    Matrix4.LookAt(lightPos, lightPos + new Vector3(0.0f, 0.0f, -1.0f), new Vector3(0.0f, -1.0f, 0.0f)) * shadowProj
                };

                depthShader.Use();
                for(int z = 0; z < 6; ++z)
                    depthShader.SetMatrix4("shadowMatrices[" + z + "]", shadowTransforms[z]);
                depthShader.SetVec3("lightPos", lightPos);
                depthShader.SetFloat("far_plane", far_plane);
                Matrix4 modelMatrix = Matrix4.CreateScale(0.1f);
                depthShader.SetMatrix4("modelMatrix", modelMatrix);

                GL.Enable(EnableCap.PolygonOffsetFill);
                GL.PolygonOffset(1.1f, 1.5f);
                foreach(Model m in models)
                    m.Draw(depthShader);
                GL.Disable(EnableCap.PolygonOffsetFill);

                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

                targetShader.Use();
                targetShader.SetMatrix4("cubeProjMatrix", shadowProj);
                GL.ActiveTexture(TextureUnit.Texture10 + i);
                GL.BindTexture(TextureTarget.TextureCubeMap, shadowCubemaps[i]);
                targetShader.SetInt("depthMaps[" + i + "]", 10 + i);
                CheckLastError();
                GL.DeleteFramebuffer(shadowFBOs[i]);
                //GL.DeleteTexture(shadowCubemaps[i]);
            }
        }
    }
}
