using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Components;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.JSInterop;
using babylon_wasm.Client;
using babylon_wasm.Client.Shared;
using System;
using System.Threading.Tasks;
using BABYLON;
using EventHorizon.Blazor.Interop.Callbacks;
using Microsoft.AspNetCore.Components.Web;
//using EventHorizon.Blazor.Server.Interop.Callbacks;

namespace babylon_wasm.Client.Pages
{
    public partial class Index : IDisposable
    {
        private Engine _engine;
        private DebugLayerScene _scene;
        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {

            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await CreateSceneAsync();
            }
        }

        public void Dispose()
        {
            _engine?.dispose();
        }

        HemisphericLight light;
        FollowCamera camera;

        public async Task CreateSceneAsync()
        {

            var canvas = Canvas.GetElementById("game-window");
            var engine = new Engine(canvas, true);
            // extend layer scene untuk debug
            _scene = new DebugLayerScene(engine);

            #region follow cam
            // bikin kamera yang ngikutin dan set posisi awal kamera 	
            camera = new BABYLON.FollowCamera("FollowCam", new BABYLON.Vector3(-6, 0, 0), _scene);

            //jarak kamera ke target 
            camera.radius = 1;

            // tinggi kamera dari target
            camera.heightOffset = 8;

            // rotasi kamera dari tengah target di x y plane
            camera.rotationOffset = 0;

            //akselerasi kamera dari posisi saat ini ke target
            camera.cameraAcceleration = 0.005m;

            //maksimum speed kamera 
            camera.maxCameraSpeed = 10m;

            //tempel kamera ke canvas
            camera.attachControl(true);
            #endregion

            //untuk kamera yang berotasi
            #region arc rotate cam
            //var camera = new BABYLON.ArcRotateCamera("camera", (decimal)(-System.Math.PI / 2), (decimal)(System.Math.PI / 2.5), 15, new BABYLON.Vector3(0, 0, 0), _scene);
            //camera.upperBetaLimit = (decimal)(System.Math.PI / 2.2);
            //camera.attachControl(true);
            #endregion

            light = new BABYLON.HemisphericLight("light", new BABYLON.Vector3(1, 1, 0), _scene);
            light.intensity = 1;

            //BuildDwelling(_scene);
            AddVillageSkyTrees(_scene);
            AddFountain(_scene);
            AddLights(_scene);
            await AnimateCharacterAsync(_scene);


            //kalau pake vr device biasa pake freecamera
            #region vr cam
            /*
            var camera = new BABYLON.FreeCamera("camera1", new BABYLON.Vector3(0, 5, -6), _scene);
            camera.setTarget(BABYLON.Vector3.Zero());
            camera.attachControl(true);
            var light = new BABYLON.HemisphericLight("light", new BABYLON.Vector3(1, 1, 0), _scene);
            */
            #endregion

            // Default Environment
            /*
             const env = scene.createDefaultEnvironment();

            const xr = await scene.createDefaultXRExperienceAsync({
                floorMeshes: [env.ground]
            });
             */

            var supportVR = await BABYLON.WebXRSessionManager.IsSessionSupportedAsync("immersive-vr");
            if (supportVR)
            {


            }

            var xr = await WebXRExperienceHelper.CreateAsync(_scene);
            var adt = BABYLON.GUI.AdvancedDynamicTexture.CreateFullscreenUI("UI");
            var btn = BABYLON.GUI.Button.CreateSimpleButton("button", "Toggle XR from GUI button");
            btn.horizontalAlignment = BABYLON.GUI.Control.HORIZONTAL_ALIGNMENT_LEFT;
            btn.verticalAlignment = BABYLON.GUI.Control.VERTICAL_ALIGNMENT_BOTTOM;
            btn.textBlock.color = "white";
            btn.width = "200px";
            btn.height = "50px";
            btn.onPointerClickObservable.add((i, e) => {
                /*
                if (xr!=null && xr.baseExperience)
                {
                    if (xr.baseExperience.state === BABYLON.WebXRState.NOT_IN_XR)
                        xr.baseExperience.enterXRAsync("immersive-vr", "local-floor");
                    else
                        xr.baseExperience.exitXRAsync();
                }*/
                return Task.CompletedTask;
            });
            adt.addControl(btn);

            //var res = xrHelper?.enterXRAsync("immersive-vr", "local-floor");

            engine.runRenderLoop(new ActionCallback(() => Task.Run(() => _scene.render(true, false))));

            _engine = engine;
        }

        #region Lights and Shadow

        void AddLights(Scene scene)
        {
            // GUI

            var adt = BABYLON.GUI.AdvancedDynamicTexture.CreateFullscreenUI("UI", true, scene: scene);

            var panel = new BABYLON.GUI.StackPanel("stack1");
            panel.width = "220px";
            panel.top = "-25px";
            panel.horizontalAlignment = BABYLON.GUI.Control.HORIZONTAL_ALIGNMENT_RIGHT;
            panel.verticalAlignment = BABYLON.GUI.Control.VERTICAL_ALIGNMENT_BOTTOM;
            adt.addControl(panel);

            var header = new BABYLON.GUI.TextBlock("txt1");
            header.text = "Night to Day";
            header.height = "30px";
            header.color = "white";
            panel.addControl(header);

            var slider = new BABYLON.GUI.Slider("slider1");
            slider.minimum = 0;
            slider.maximum = 1;
            slider.borderColor = "black";
            slider.color = "gray";
            slider.background = "white";
            slider.value = 1;
            slider.height = "20px";
            slider.width = "200px";

            slider.onPointerMoveObservable.add((x, y) =>
            {
                if (light != null)
                {
                    light.intensity = slider.value;
                }
                return Task.CompletedTask;
            });
            panel.addControl(slider);

            BABYLON.SceneLoader.ImportMesh("", "https://assets.babylonjs.com/meshes/", "lamp.babylon", scene,
                new ActionCallback<AbstractMesh[], IParticleSystem[], Skeleton[], AnimationGroup[], TransformNode[], Geometry[], Light[]>(
                    (meshes, arg2, skeletons, arg4, arg5, arg6, arg7) =>
                    {
                        var bulb = scene.getMeshByName("bulb");
                        var lampLight = new BABYLON.SpotLight("lampLight", BABYLON.Vector3.Zero(), new BABYLON.Vector3(0, -1, 0), (decimal)(0.8 * System.Math.PI), 0.01m, scene);
                        lampLight.diffuse = BABYLON.Color3.Yellow();
                        lampLight.parent = bulb;


                        var lamp = scene.getMeshByName("lamp");
                        lamp.position = new BABYLON.Vector3(2, 0, 2);
                        lamp.rotation = BABYLON.Vector3.Zero();
                        lamp.rotation.y = (decimal)(-System.Math.PI / 4);

                        var lamp3 = lamp.clone("lamp3", null);
                        lamp3.position.z = -8;

                        var lamp1 = lamp.clone("lamp1", null);
                        lamp1.position.x = -8;
                        lamp1.position.z = 1.2m;
                        lamp1.rotation.y = (decimal)(System.Math.PI / 2);

                        var lamp2 = lamp1.clone("lamp2", null);
                        lamp2.position.x = -2.7m;
                        lamp2.position.z = 0.8m;
                        lamp2.rotation.y = (decimal)(-System.Math.PI / 2);


                        return Task.CompletedTask;
                    }));
        }

        #endregion

        #region fountain

        void AddFountain(Scene scene)
        {
            ParticleSystem particleSystem;
            AbstractMesh fountain;
            //nyalain air mancur
            bool switched = false;
            void pointerDown(AbstractMesh mesh)
            {
                if (mesh.name == fountain.name)
                {
                    switched = !switched;
                    if (switched)
                    {
                        // mulai particle system
                        particleSystem.start();
                    }
                    else
                    {
                        // Stop particle system
                        particleSystem.stop();
                    }
                }

            }
            // bikin particle system
            particleSystem = new BABYLON.ParticleSystem("particles", 5000m, scene);

            //Texture of each particle
            particleSystem.particleTexture = new BABYLON.Texture(scene, "textures/flare.png");

            // titik keluarnya partikel
            var emiter1 = new AbstractMesh("emiter", scene);
            emiter1.position = new BABYLON.Vector3(-4m, 0.8m, -6m); // emitted from the top of the fountain
            particleSystem.emitter = emiter1;
            particleSystem.minEmitBox = new BABYLON.Vector3(-0.01m, 0, -0.01m); // Starting all from
            particleSystem.maxEmitBox = new BABYLON.Vector3(0.01m, 0, 0.01m); // To...

            // warna particles
            particleSystem.color1 = new BABYLON.Color4(0.7m, 0.8m, 1.0m, 1.0m);
            particleSystem.color2 = new BABYLON.Color4(0.2m, 0.5m, 1.0m, 1.0m);

            // ukuran partiker dari sampai
            particleSystem.minSize = 0.01m;
            particleSystem.maxSize = 0.05m;

            // life dari setiap partikel (random antara...
            particleSystem.minLifeTime = 0.3m;
            particleSystem.maxLifeTime = 1.5m;

            // rata-rata keluar partikel
            particleSystem.emitRate = 1500;

            // Blend mode : BLENDMODE_ONEONE, or BLENDMODE_STANDARD
            particleSystem.blendMode = BABYLON.ParticleSystem.BLENDMODE_ONEONE;

            // set gravitasi partikel
            particleSystem.gravity = new BABYLON.Vector3(0, -9.81m, 0);

            // arah partikel setelah keluar
            particleSystem.direction1 = new BABYLON.Vector3(-1, 8, 1);
            particleSystem.direction2 = new BABYLON.Vector3(1, 8, -1);

            // Power dan speed
            particleSystem.minEmitPower = 0.2m;
            particleSystem.maxEmitPower = 0.6m;
            particleSystem.updateSpeed = 0.01m;

            var fountainProfile = new Vector3[] {
               new BABYLON.Vector3(0, 0, 0),
               new BABYLON.Vector3(0.5m, 0, 0),
               new BABYLON.Vector3(0.5m, 0.2m, 0),
               new BABYLON.Vector3(0.4m, 0.2m, 0),
               new BABYLON.Vector3(0.4m, 0.05m, 0),
               new BABYLON.Vector3(0.05m, 0.1m, 0),
               new BABYLON.Vector3(0.05m, 0.8m, 0),
               new BABYLON.Vector3(0.15m, 0.9m, 0)
            };
            //bikin pancuran
            fountain = BABYLON.MeshBuilder.CreateLathe("fountain", new { shape = fountainProfile, sideOrientation = BABYLON.Mesh.DOUBLESIDE }, scene);
            fountain.position.x = -4;
            fountain.position.z = -6;



            scene.onPointerObservable.add((pointerInfo, event1) =>
            {
                if (pointerInfo.type == BABYLON.PointerEventTypes.POINTERDOWN)
                {
                    if (pointerInfo.pickInfo.hit)
                    {
                        pointerDown(pointerInfo.pickInfo.pickedMesh);
                    }
                }
                return Task.CompletedTask;
            });

        }
        #endregion

        #region tree
        void AddVillageSkyTrees(Scene scene)
        {

            var spriteManagerTrees = new BABYLON.SpriteManager("treesManager", "textures/palm.png", 2000, new { width = 512, height = 1024 }, scene);
            Random rnd = new Random();
            //bikin pohon di lokasi random
            for (var i = 0; i < 500; i++)
            {
                var tree = new BABYLON.Sprite("tree", spriteManagerTrees);
                tree.position.x =(decimal)( rnd.NextDouble() * (-30));
                tree.position.z = (decimal)(rnd.NextDouble() * 20 + 8);
                tree.position.y = 0.5m;
            }

            for (var i = 0; i < 500; i++)
            {
                var tree = new BABYLON.Sprite("tree", spriteManagerTrees);
                tree.position.x = (decimal)(rnd.NextDouble() * (25) + 7);
                tree.position.z = (decimal)(rnd.NextDouble() * -35 + 8);
                tree.position.y = 0.5m;
            }

            //Skybox
            var skybox = BABYLON.MeshBuilder.CreateBox("skyBox", new { size = 150 }, scene);
            var skyboxMaterial = new BABYLON.StandardMaterial("skyBox", scene);
            skyboxMaterial.backFaceCulling = false;
            skyboxMaterial.reflectionTexture = new BABYLON.CubeTexture("textures/skybox", scene);
            skyboxMaterial.reflectionTexture.coordinatesMode = BABYLON.Texture.SKYBOX_MODE;
            skyboxMaterial.diffuseColor = new BABYLON.Color3(0, 0, 0);
            skyboxMaterial.specularColor = new BABYLON.Color3(0, 0, 0);
            skybox.material = skyboxMaterial;
            //village
            BABYLON.SceneLoader.ImportMesh("", "https://assets.babylonjs.com/meshes/", "valleyvillage.glb", scene);
            //mobil
            BABYLON.SceneLoader.ImportMesh("", "https://assets.babylonjs.com/meshes/", "car.glb", scene,
                new ActionCallback<AbstractMesh[], IParticleSystem[], Skeleton[], AnimationGroup[], TransformNode[], Geometry[], Light[]>(
                    (meshes, arg2, skeletons, arg4, arg5, arg6, arg7) =>
                    {
                        var car = scene.getMeshByName("car");
                        car.rotation = new BABYLON.Vector3((decimal)(System.Math.PI / 2), 0, (decimal)(-System.Math.PI / 2));
                        car.position.y = 0.16m;
                        car.position.x = -3m;
                        car.position.z = 8m;

                        var animCar = new BABYLON.Animation("carAnimation", "position.z", 30, BABYLON.Animation.ANIMATIONTYPE_FLOAT, BABYLON.Animation.ANIMATIONLOOPMODE_CYCLE);
                        /*
                        var carKeys = new List<IAnimationKeyCachedEntity>();

                        carKeys.Add(new IAnimationKeyCachedEntity()
                        {
                            frame = 0,
                            value = 10
                        });
                        carKeys.Add(new IAnimationKeyCachedEntity()
                        {
                            frame = 200,
                            value = -15
                        });

                        animCar.setKeys(carKeys.ToArray());
                        */
                        //car.animations = [];
                        car.animations = new Animation[1] { animCar };
                        //car.createAnimationRange("carAnimation", 0, 200);
                        //car.beginAnimation("carAnimation", true, 1);

                        //scene.beginAnimation(car, 0, 200, true);

                        //wheel animation
                        var wheelRB = scene.getMeshByName("wheelRB");
                        var wheelRF = scene.getMeshByName("wheelRF");
                        var wheelLB = scene.getMeshByName("wheelLB");
                        var wheelLF = scene.getMeshByName("wheelLF");

                        wheelRB.createAnimationRange("x1", 0, 30);
                        wheelRB.beginAnimation("x1", true, 1);
                        wheelRF.createAnimationRange("x2", 0, 30);
                        wheelRF.beginAnimation("x2", true, 1);
                        wheelLB.createAnimationRange("x3", 0, 30);
                        wheelLB.beginAnimation("x3", true, 1);
                        wheelLF.createAnimationRange("x4", 0, 30);
                        wheelLF.beginAnimation("x4", true, 1);

                        //scene.beginAnimation(wheelRF, 0, 30, true);
                        //scene.beginAnimation(wheelLB, 0, 30, true);
                        //scene.beginAnimation(wheelLF, 0, 30, true);

                        return Task.CompletedTask;
                    }));

        }
        #endregion

        #region character
        
        class walk
        {
            public decimal turn { get; set; }
            public decimal dist { get; set; }
            public walk(decimal Turn, decimal Dist)
            {
                this.turn = Turn;
                this.dist = Dist;

            }
        }
        async Task AnimateCharacterAsync(Scene scene)
        {
            
            var track = new List<walk>();
            track.Add(new walk(86m, 7m));
            track.Add(new walk(-85m, 14.8m));
            track.Add(new walk(-93m, 16.5m));
            track.Add(new walk(48m, 25.5m));
            track.Add(new walk(-112m, 30.5m));
            track.Add(new walk(-72m, 33.2m));
            track.Add(new walk(42m, 37.5m));
            track.Add(new walk(-98m, 45.2m));
            track.Add(new walk(0m, 47m));

            var _animationMap = new Dictionary<string, AnimationGroup>();
            AnimationGroup _runningAnimation;
            // Dude
            var result = BABYLON.SceneLoader.ImportMesh("him", "/scenes/", "Dude.babylon", scene,
               new ActionCallback<AbstractMesh[], IParticleSystem[], Skeleton[], AnimationGroup[], TransformNode[], Geometry[], Light[]>((meshes, arg2, skeletons, arg4, arg5, arg6, arg7) =>
               {

                   foreach (var animation in arg4)
                   {
                       animation.stop();
                       _animationMap.Add(animation.name, animation);
                   }
                   if (_animationMap.Count > 0)
                   {
                       _runningAnimation = _animationMap.First().Value;
                       _runningAnimation.start(true);
                   }

                   var dude = meshes[0];
                   dude.scaling = new BABYLON.Vector3(0.008m, 0.008m, 0.008m);

                   dude.position = new BABYLON.Vector3(-6, 0, 0);
                   dude.rotate(BABYLON.Axis.Y, BABYLON.Tools.ToRadians(-95));
                   var startRotation = dude.rotationQuaternion.clone();
                   if (skeletons != null && skeletons.Length > 0)
                   {
                       skeletons[0].createAnimationRange("anim", 0, 100);
                       skeletons[0].beginAnimation("anim", true, 1);
                   }
                   //scene.beginanimation(result.skeletons, 0, 100, true, 1.0);

                   var distance = 0m;
                   var step = 0.005m;
                   var p = 0;
                   camera.lockedTarget = dude;
                   scene.onBeforeRenderObservable.add((_s, _e) =>
                   {

                       dude.movePOV(0, 0, step);
                       distance += step;

                       if (distance > track[p].dist)
                       {

                           dude.rotate(BABYLON.Axis.Y, BABYLON.Tools.ToRadians(track[p].turn));//BABYLON.Space.LOCAL
                           p += 1;
                           p %= track.Count;
                           if (p == 0m)
                           {
                               distance = 0;
                               dude.position = new BABYLON.Vector3(-6, 0, 0);
                               dude.rotationQuaternion = startRotation.clone();
                           }
                       }
                       return Task.CompletedTask;
                   });

                   return Task.CompletedTask;

               }));




        }
        #endregion

        #region village
        Mesh buildHouse(int width, Scene scene)
        {
            var box = buildBox(width, scene);
            var roof = buildRoof(width, scene);

            var mesh = BABYLON.Mesh.MergeMeshes(new Mesh[] { box, roof }, true, false, null, false, true);
            return mesh;
        }
        Mesh buildBox(int width, Scene scene)
        {
            //texture
            var boxMat = new BABYLON.StandardMaterial("boxMat", scene);
            if (width == 2)
            {
                boxMat.diffuseTexture = new BABYLON.Texture(scene, "https://assets.babylonjs.com/environments/semihouse.png");
            }
            else
            {
                boxMat.diffuseTexture = new BABYLON.Texture(scene, "https://assets.babylonjs.com/environments/cubehouse.png");
            }

            //parameter untuk atur gambar di setiap sisi
            var faceUV = new Vector4[4];
            if (width == 2)
            {
                faceUV[0] = new BABYLON.Vector4(0.6m, 0.0m, 1.0m, 1.0m); //rear face
                faceUV[1] = new BABYLON.Vector4(0.0m, 0.0m, 0.4m, 1.0m); //front face
                faceUV[2] = new BABYLON.Vector4(0.4m, 0, 0.6m, 1.0m); //right side
                faceUV[3] = new BABYLON.Vector4(0.4m, 0, 0.6m, 1.0m); //left side
            }
            else
            {
                faceUV[0] = new BABYLON.Vector4(0.5m, 0.0m, 0.75m, 1.0m); //rear face
                faceUV[1] = new BABYLON.Vector4(0.0m, 0.0m, 0.25m, 1.0m); //front face
                faceUV[2] = new BABYLON.Vector4(0.25m, 0, 0.5m, 1.0m); //right side
                faceUV[3] = new BABYLON.Vector4(0.75m, 0, 1.0m, 1.0m); //left side
            }
            // top 4 dan bottom 5 tidak terlihat jadi ga di atur


            var box = BABYLON.MeshBuilder.CreateBox("box", new { width = width, faceUV = faceUV, wrap = true }, scene);
            box.material = boxMat;
            box.position.y = 0.5m;

            return box;
        }


        Mesh buildRoof(int width, Scene scene)
        {
            //texture
            var roofMat = new BABYLON.StandardMaterial("roofMat", scene);
            roofMat.diffuseTexture = new BABYLON.Texture(scene, "https://assets.babylonjs.com/environments/roof.jpg");

            var roof = BABYLON.MeshBuilder.CreateCylinder("roof", new { diameter = 1.3, height = 1.2, tessellation = 3 }, scene);
            roof.material = roofMat;
            roof.scaling.x = 0.75m;
            roof.scaling.y = width;
            roof.rotation.z = (decimal)(System.Math.PI / 2);
            roof.position.y = 1.22m;

            return roof;
        }
        void buildGround(Scene scene)
        {
            //color
            var groundMat = new BABYLON.StandardMaterial("groundMat", scene);
            groundMat.diffuseColor = new BABYLON.Color3(0, 1, 0);

            var ground = BABYLON.MeshBuilder.CreateGround("ground", new { width = 15, height = 16 }, scene);
            ground.material = groundMat;
        }
        void BuildDwelling(Scene scene)
        {
            buildGround(scene);

            var detached_house = buildHouse(1, scene);
            detached_house.rotation.y = (decimal)(-System.Math.PI / 16);
            detached_house.position.x = -6.8m;
            detached_house.position.z = 2.5m;

            var semi_house = buildHouse(2, scene);
            semi_house.rotation.y = (decimal)(-System.Math.PI / 16);
            semi_house.position.x = -4.5m;
            semi_house.position.z = 3m;

            var places = new List<decimal[]>(); //each entry is an array [house type, rotation, x, z]
            places.Add(new[] { 1m, (decimal)(-System.Math.PI / 16), -6.8m, 2.5m });
            places.Add(new[] { 2m, (decimal)(-System.Math.PI / 16), -4.5m, 3m });
            places.Add(new[] { 2m, (decimal)(-System.Math.PI / 16), -1.5m, 4m });
            places.Add(new[] { 2m, (decimal)(-System.Math.PI / 3), 1.5m, 6m });
            places.Add(new[] { 2m, (decimal)(15 * System.Math.PI / 16), -6.4m, -1.5m });
            places.Add(new[] { 1m, (decimal)(15 * System.Math.PI / 16), -4.1m, -1m });
            places.Add(new[] { 2m, (decimal)(15 * System.Math.PI / 16), -2.1m, -0.5m });
            places.Add(new[] { 1m, (decimal)(5 * System.Math.PI / 4), 0, -1 });
            places.Add(new[] { 1m, (decimal)(System.Math.PI + System.Math.PI / 2.5), 0.5m, -3m });
            places.Add(new[] { 2m, (decimal)(System.Math.PI + System.Math.PI / 2.1), 0.75m, -5m });
            places.Add(new[] { 1m, (decimal)(System.Math.PI + System.Math.PI / 2.25), 0.75m, -7m });
            places.Add(new[] { 2m, (decimal)(System.Math.PI / 1.9), 4.75m, -1m });
            places.Add(new[] { 1m, (decimal)(System.Math.PI / 1.95), 4.5m, -3m });
            places.Add(new[] { 2m, (decimal)(System.Math.PI / 1.9), 4.75m, -5m });
            places.Add(new[] { 1m, (decimal)(System.Math.PI / 1.9), 4.75m, -7m });
            places.Add(new[] { 2m, (decimal)(-System.Math.PI / 3), 5.25m, 2m });
            places.Add(new[] { 1m, (decimal)(-System.Math.PI / 3), 6m, 4m });

            //bikin rumah berdasarkan dua rumah yang awal dibuat 
            var houses = new InstancedMesh[places.Count];
            for (var i = 0; i < places.Count; i++)
            {
                if (places[i][0] == 1m)
                {
                    houses[i] = detached_house.createInstance("house" + i);
                }
                else
                {
                    houses[i] = semi_house.createInstance("house" + i);
                }
                houses[i].rotation.y = places[i][1];
                houses[i].position.x = places[i][2];
                houses[i].position.z = places[i][3];
            }
        }
        #endregion
        protected void HandleKeyDown(KeyboardEventArgs args)
        {
            Console.WriteLine(args.Key);
            if (args.ShiftKey && args.CtrlKey && args.AltKey && args.Key.ToLower() == "i")
            {
                if (_scene.debugLayer.isVisible())
                {
                    Console.WriteLine("Hello");
                    _scene.debugLayer.hide();
                }
                else
                {
                    _scene.debugLayer.show();
                }
            }
        }
    }
}