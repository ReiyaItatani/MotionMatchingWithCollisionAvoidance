## 1.6.4 (2022-09-17)
    - \reflink{CommandBuilder.DisposeAfter} will now block on the given dependency before rendering the current frame by default.
        This reduces the risk of flickering when using ECS systems as they may otherwise not have completed their work before the frame is rendered.
        You can pass \reflink{AllowedDelay.Infinite} to disable this behavior for long-running jobs.
    - Fixed recent regression causing drawing to fail in standalone builds.

## 1.6.3 (2022-09-15)
    - Added \reflink{LabelAlignment.withPixelOffset}.
    - Fixed \reflink{LabelAlignment} had top and bottom alignment swapped. So for example \reflink{LabelAlignment.TopLeft} was actually \reflink{LabelAlignment.BottomLeft}.
    - Fixed shaders would sometimes cause compilation errors, especially if you changed render pipelines.
    - Improved sharpness of \reflink{Draw.Label2D} and \reflink{Draw.Label3D} when using small font-sizes.\n
        <table>
        <tr><td>Before</td><td>After</td></tr>
        <tr>
        <td>
        \shadowimage{changelog/text_blurry_small.png}
        </td>
        <td>
        \shadowimage{changelog/text_sharp_small.png}
        </td>
        </table>
    - Text now fades out slightly when behind or inside other objects. The fade out amount can be controlled in the project settings:
        \shadowimage{changelog/text_opacity.png}
    - Fixed \reflink{Draw.Label2D} and \reflink{Draw.Label3D} font sizes would be incorrect (half as large) when the camera was in orthographic mode.
    - Fixed \reflink{Draw.WireCapsule} and \reflink{Draw.WireCylinder} would render incorrectly in certain orientations.

## 1.6.2 (2022-09-05)
    - Fix typo causing prefabs to always be drawn in the scene view in Unity versions earlier than 2022.1, even if they were not even added to the scene.

## 1.6.1 (2022-08-31)
    - Fix vertex buffers not getting resized correctly. This could cause exceptions to be logged sometimes. Regression in 1.6.

## 1.6 (2022-08-27)
    - Fixed documentation and changelog URLs in the package manager.
    - Fixed dragging a prefab into the scene view would instantiate it, but gizmos for scripts attached to it would not work.
    - Fixed some edge cases in \reflink{Draw.WireCapsule} and \reflink{Draw.WireCapsule} which could cause NaNs and other subtle errors.
    - Improved compatibility with WebGL as well as Intel GPUs on Mac.
    - Added warning when using HDRP and custom passes are disabled.
    - Improved performance of watching for destroyed objects.
    - Reduced overhead when having lots of objects inheriting from \reflink{MonoBehaviourGizmos}.
    - It's now possible to enable/disable gizmos for component types via the Unity Scene View Gizmos menu when using render pipelines in Unity 2022.1+.
        In earlier versions of Unity, a limited API made this impossible.
    - Made it possible to adjust the global opacity of gizmos in the Unity Project Settings.
        \shadowimage{changelog/settings.png}

## 1.5.3 (2022-05-14)
    - Breaking changes
        - The minimum supported Unity version is now 2020.3.
    - The URP 2D renderer now has support for all features required by ALINE. So the warning about it not being supported has been removed.
    - Fixed windows newlines (\n\r) would show up as a newline and a question mark instead of just a newline.
    - Fixed compilation errors when using the Unity.Collections package between version 0.8 and 0.11.
    - Improved performance in some edge cases.
    - Fixed \reflink{Draw.SolidMesh} with a non-white color could affect the color of unrelated rendered lines. Thanks Chris for finding and reporting the bug.
    - Fixed an exception could be logged when drawing circles with a zero or negative line width.
    - Fixed various compilation errors that could show up when using newer versions of the burst package.

## 1.5.2 (2021-11-09)
    - Fix gizmos would not show up until you selected the camera if you had just switched to the universal render pipeline.
    - Improved performance of drawing lines by more efficiently sending the data to the shader.
        This has the downside that shader target 4.5 is now required. I don't think this should be a big deal nowadays, but let me know if things don't work on your platform.
        This was originally introduced in 1.5.0, but reverted in 1.5.1 due to some compatibility issues causing rendering to fail for some project configurations. I think those issues should be resolved now.

## 1.5.1 (2021-10-28)
    - Reverted "Improved performance of drawing lines by more efficiently sending the data to the shader." from 1.5.0.
        It turns out this caused issues for some users and could result in gizmos not showing at all.
        I'll try to figure out a solution and bring the performance improvements back.

## 1.5 (2021-10-27)
    - Added support FixedStrings in \reflink{Draw.Label2D(float3,FixedString32Bytes,float)}, which means it can be used inside burst jobs (C# managed strings cannot be used in burst jobs).
    - Fixed a 'NativeArray has not been disposed' error message that could show up if the whole project's assets were re-imported.
    - Added \reflink{Draw.SolidCircle}.
       \shadowimage{rendered/solidcircle.png}
    - Added \reflink{Draw.SolidCircleXZ}.
       \shadowimage{rendered/solidcirclexz.png}
    - Added \reflink{Draw.SolidArc}.
       \shadowimage{rendered/solidarc.png}
    - Added \reflink{Draw.Label3D}
        \shadowimage{rendered/label3d.png}
    - Improved performance of \reflink{Draw.WirePlane} and \reflink{Draw.WireRectangle} by making them primitives instead of just calling \reflink{Draw.Line} 4 times.
    - Improved performance in general by more efficiently re-using existing vertex buffers.
    - Fixed some warnings related to ENABLE_UNITY_COLLECTIONS_CHECKS which burst would log when building a standalone player.
    - Changed more functions in the \reflink{Draw} class to take a Unity.Mathematics.quaternion instead of a UnityEngine.Quaternion.
        Implicit conversions exist in both directions, so there is no need to change your code.

## 1.4.3 (2021-09-04)
    - Fixed some debug printout had been included by mistake. A "Disposing" message could sometimes show up in the console.

## 1.4.2 (2021-08-22)
    - Reduced overhead in standalone builds if you have many objects in the scene.
    - Fixed \reflink{Draw.WireCapsule(float3,float3,float)} could render incorrectly if the start and end parameters were identical.
    - Fixed \reflink{Draw.WithDuration} scopes could survive until the next time the game started if no game or scene cameras were ever rendered while in edit mode.
    - Added \reflink{Draw.SphereOutline(float3,float)}.
       \shadowimage{rendered/sphereoutline.png}
    - \reflink{Draw.WireSphere(float3,float)} has changed to always include an outline of the sphere. This makes it a lot nicer to look at.
       \shadowimage{rendered/wiresphere.png}

## 1.4.1 (2021-02-28)
    - Added \reflink{CommandBuilder.DisposeAfter} to dispose a command builder after a job has completed.
    - Fixed gizmos would be rendered for other objects when the scene view was in prefab isolation mode. Now they will be hidden, which matches what Unity does.
    - Fixed a deprecation warning when unity the HDRP package version 9.0 or higher.
    - Improved docs for \reflink{RedrawScope}.
    - Fixed documentation for scopes (e.g. \reflink{Draw.WithColor}) would show up as missing in the online documentation.

## 1.4 (2021-01-27)
    - Breaking changes
        - \reflink{Draw.WireCapsule(float3,float3,float)} with the bottom/top parameterization was incorrect and the behavior did not match the documentation for it.
            This method has been changed so that it now matches the documentation as this was the intended behavior all along.
            The documentation and parameter names have also been clarified.
    - Added \reflink{Draw.SolidRectangle(Rect)}.
    - Fixed \reflink{Draw.SolidBox(float3,quaternion,float3)} and \reflink{Draw.WireBox(float3,quaternion,float3)} rendered a box that was offset by 0.5 times the size of the box.
        This bug only applied to the overload with a rotation, not for example to \reflink{Draw.SolidBox(float3,float3)}.
    - Fixed Draw.SolidMesh would always be rendered at the world origin with a white color. Now it picks up matrices and colors properly.
    - Fixed a bug which could cause a greyed out object called 'RetainedGizmos' to appear in the scene hierarchy.
    - Fixed some overloads of WireCylinder, WireCapsule, WireBox and SolidBox throwing errors when you tried to use them in a Burst job.
    - Improved compatibility with some older versions of the Universal Render Pipeline.

## 1.3.1 (2020-10-10)
    - Improved performance in standalone builds by more aggressively compiling out drawing commands that would never render anything anyway.
    - Reduced overhead in some cases, in particular when nothing is being rendered.

## 1.3 (2020-09-12)
    - Added support for line widths.
        See \reflink{Draw.WithLineWidth}.
        \shadowimage{features/line_widths.png}
    - Added warning message when using the Experimental URP 2D Renderer. The URP 2D renderer unfortunately does not have enough features yet
        to be able to support ALINE. It doesn't have an extensible post processing system. The 2D renderer will be supported as soon as it is technically possible.
    - Fixed \reflink{Draw.SolidPlane(float3,float3,float2)} and \reflink{Draw.WirePlane(float3,float3,float2)} not working for all normals.
    - Fixed the culling bounding box for text and lines could be calculated incorrectly if text labels were used.
        This could result in text and lines randomly disappearing when the camera was looking in particular directions.
    - Renamed \reflink{Draw.PushPersist} and \reflink{Draw.PopPersist} to \reflink{Draw.PushDuration} and \reflink{Draw.PopDuration} for consistency with the \reflink{Draw.WithDuration} scope.
        The previous names will still work, but they are marked as deprecated.
    - Known bugs
        - \reflink{Draw.SolidMesh(Mesh)} does not respect matrices and will always be drawn with the pivot at the world origin.

## 1.2.3 (2020-07-26)
    - Fixed solid drawing not working when using VR rendering.
    - Fixed nothing was visible when using the Universal Render Pipeline and post processing was enabled.
        Note that ALINE will render before post processing effects when using the URP.
        This is because as far as I can tell the Universal Render Pipeline does not expose any way to render objects
        after post processing effects because it renders to hidden textures that custom passes cannot access.
    - Fixed drawing sometimes not working when using the High Definition Render Pipeline.
        In contrast to the URP, ALINE can actually render after post processing effects with the HDRP since it has a nicer API. So it does that.
    - Known bugs
        - \reflink{Draw.SolidMesh(Mesh)} does not respect matrices and will always be drawn with the pivot at the world origin.

## 1.2.2 (2020-07-11)
    - Added \reflink{Draw.Arc(float3,float3,float3)}.
        \shadowimage{rendered/arc.png}
    - Fixed drawing sometimes not working when using the Universal Render Pipeline, in particular when either HDR or anti-aliasing was enabled.
    - Fixed drawing not working when using VR rendering.
    - Hopefully fixed the issue that could sometimes cause "The ALINE package installation seems to be corrupt. Try reinstalling the package." to be logged when first installing
        the package (even though the package wasn't corrupt at all).
    - Incremented required burst package version from 1.3.0-preview.7 to 1.3.0.
    - Fixed the offline documentation showing the wrong page instead of the get started guide.

## 1.2.1 (2020-06-21)
    - Breaking changes
        - Changed the size parameter of Draw.WireRect to be a float2 instead of a float3.
            It made no sense for it to be a float3 since a rectangle is two-dimensional. The y coordinate of the parameter was never used.
    - Added <a href="ref:Draw.WirePlane(float3,float3,float2)">Draw.WirePlane</a>.
        \shadowimage{rendered/wireplane.png}
    - Added <a href="ref:Draw.SolidPlane(float3,float3,float2)">Draw.SolidPlane</a>.
        \shadowimage{rendered/solidplane.png}
    - Added <a href="ref:Draw.PlaneWithNormal(float3,float3,float2)">Draw.PlaneWithNormal</a>.
        \shadowimage{rendered/planewithnormal.png}
    - Fixed Drawing.DrawingUtilities class missed an access modifier. Now all methods are properly public and can be accessed without any issues.
    - Fixed an error could be logged after using the WireMesh method and then exiting/entering play mode.
    - Fixed Draw.Arrow not drawing the arrowhead properly when the arrow's direction was a multiple of (0,1,0).

## 1.2 (2020-05-22)
    - Added page showing some advanced usages: \ref advanced.
    - Added \link Drawing.Draw.WireMesh Draw.WireMesh\endlink.
        \shadowimage{rendered/wiremesh.png}
    - Added \link Drawing.CommandBuilder.cameraTargets CommandBuilder.cameraTargets\endlink.
    - The WithDuration scope can now be used even outside of play mode. Outside of play mode it will use Time.realtimeSinceStartup to measure the duration.
    - The WithDuration scope can now be used inside burst jobs and on different threads.
    - Fixed WireCylinder and WireCapsule logging a warning if the normalized direction from the start to the end was exactly (1,1,1).normalized. Thanks Billy Attaway for reporting this.
    - Fixed the documentation showing the wrong namespace for classes. It listed \a Pathfinding.Drawing but the correct namespace is just \a %Drawing.

## 1.1.1 (2020-05-04)
    - Breaking changes
        - The vertical alignment of Label2D has changed slightly. Previously the Top and Center alignments were a bit off from the actual top/center.
    - Fixed conflicting assembly names when used in a project that also has the A* Pathfinding Project package installed.
    - Fixed a crash when running on iOS.
    - Improved alignment of \link Drawing.Draw.Label2D Draw.Label2D\endlink when using the Top or Center alignment.

## 1.1 (2020-04-20)
    - Added \link Drawing.Draw.Label2D Draw.Label2D\endlink which allows you to easily render text from your code.
        It uses a signed distance field font renderer which allows you to render crisp text even at high resolution.
        At very small font sizes it falls back to a regular font texture.
        \shadowimage{rendered/label2d.png}
    - Improved performance of drawing lines by about 5%.
    - Fixed a potential crash after calling the Draw.Line(Vector3,Vector3,Color) method.

## 1.0.2 (2020-04-09)
    - Breaking changes
        - A few breaking changes may be done as the package matures. I strive to keep these to as few as possible, while still not sacrificing good API design.
        - Changed the behaviour of \link Drawing.Draw.Arrow(float3,float3,float3,float) Draw.Arrow\endlink to use an absolute size head.
            This behaviour is probably the desired one more often when one wants to explicitly set the size.
            The default Draw.Arrow(float3,float3) function which does not take a size parameter continues to use a relative head size of 20% of the length of the arrow.
            \shadowimage{rendered/arrow_multiple.png}
    - Added \link Drawing.Draw.ArrowRelativeSizeHead Draw.ArrowRelativeSizeHead\endlink which uses a relative size head.
        \shadowimage{rendered/arrowrelativesizehead.png}
    - Added \link Drawing.DrawingManager.GetBuilder DrawingManager.GetBuilder\endlink instead of the unnecessarily convoluted DrawingManager.instance.gizmos.GetBuilder.
    - Added \link Drawing.Draw.CatmullRom(List<Vector3>) Draw.CatmullRom\endlink for drawing a smooth curve through a list of points.
        \shadowimage{rendered/catmullrom.png}
    - Made it easier to draw things that are visible in standalone games. You can now use for example Draw.ingame.WireBox(Vector3.zero, Vector3.one) instead of having to create a custom command builder.
        See \ref ingame for more details.

## 1.0.1 (2020-04-06)
    - Fix burst example scene not having using burst enabled (so it was much slower than it should have been).
    - Fix text color in the SceneEditor example scene was so dark it was hard to read.
    - Various minor documentation fixes.

## 1.0 (2020-04-05)
    - Initial release
