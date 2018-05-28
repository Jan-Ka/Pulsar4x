﻿using System;
using Pulsar4X.ECSLib;
using SDL2;
using ImGuiNET;
using System.Collections.Generic;

namespace Pulsar4X.SDL2UI
{

    /// <summary>
    /// Orbit draw data.
    /// How this works:
    /// First, we set up all the static non changing variables from the entites datablobs
    /// On Setup we create a list of points for the full ellipse, as if it was at 0,0 world coordinates. 
    /// On Update we calculate where the elipse should be painted in world coordinates, and recalc any user configerable variables.
    /// On Draw we translate the points both to correct for the position in world view, and for the viewscreen. 
    /// We start drawing segments from where the planet will be, and decrease the alpha channel for each segment. only drawing the number of segments required. 
    /// </summary>
    public class OrbitDrawData : Icon
    {
        struct PointD
        {
            public double x;
            public double y;
        }
        
        #region Static properties
        OrbitDB _orbitDB;
        PositionDB _bodyPositionDB;
        PositionDB _parentPositionDB;
        float _orbitEllipseMajor;
        float _orbitEllipseSemiMaj;
        float _orbitEllipseMinor;
        float _orbitEllipseSemiMinor;
        float _orbitAngleDegrees; //the orbit is an ellipse which is rotated arround one of the focal points. 
        float _orbitAngleRadians; //the orbit is an ellipse which is rotated arround one of the focal points. 
        float _focalDistance; //distance from the center of the ellpse to one of the focal points. 
        PointD[] _points; //we calculate points around the ellipse and add them here. when we draw them we translate all the points. 
        #endregion

        #region Dynamic Properties
        //focal point of the elipse this is the point the entity is orbiting around - this will change if the parent is inturn orbiting something.
        //double _focalX;
        //double _focalY;
        //double _ctrPosX;
        //double _ctrPosY;
        float _ellipseStartArcAngleRadians;
        float _segmentArcSweepAngleRadians;
        int _numberOfDrawSegments; //this is now many segments get drawn in the ellipse, ie if the _ellipseSweepAngle or _numberOfArcSegments are less, less will be drawn.

        //user adjustable
        float _ellipseSweepAngleRadians = 4.71239f;
        byte _numberOfArcSegments = 180; //In the entire ellipse. this maybe needs adjusting for zoom level. 
        SDL.SDL_Color Colour = new SDL.SDL_Color() { r = 0, g = 0, b = 100, a = 255 };
        byte _alphaChangeAmount;
        #endregion

        internal OrbitDrawData(Entity entity): base(entity.GetDataBlob<PositionDB>())
        {
            _orbitDB = entity.GetDataBlob<OrbitDB>();
            _bodyPositionDB = entity.GetDataBlob<PositionDB>();
            if (_orbitDB.Parent == null)
            {
                _positionDB = _bodyPositionDB;
                _parentPositionDB = _positionDB;
            }
            else
            {
                _parentPositionDB = _orbitDB.Parent.GetDataBlob<PositionDB>();
                _positionDB = _parentPositionDB;
            }
            Setup();
        }

        internal OrbitDrawData(OrbitDB orbitDB, PositionDB positionDB, PositionDB parentPosDB): base(parentPosDB)
        {
            _orbitDB = orbitDB;
            _bodyPositionDB = positionDB;
            _parentPositionDB = parentPosDB;
            Setup();
        }

        private void Setup()
        {
            ShapesScaleWithZoom = true;
            _orbitEllipseSemiMaj = (float)_orbitDB.SemiMajorAxis;
            _orbitEllipseMajor = _orbitEllipseSemiMaj * 2; //Major Axis
            _orbitEllipseMinor = (float)Math.Sqrt((_orbitDB.SemiMajorAxis * _orbitDB.SemiMajorAxis) * (1 - _orbitDB.Eccentricity * _orbitDB.Eccentricity)) * 2;
            _orbitEllipseSemiMinor = _orbitEllipseMinor * 0.5f;
            _orbitAngleDegrees = (float)Angle.NormaliseDegrees(_orbitDB.LongitudeOfAscendingNode + _orbitDB.ArgumentOfPeriapsis * 2); //This is the LoP + AoP.
            _orbitAngleRadians = (float)Angle.NormaliseRadians(_orbitAngleDegrees * Math.PI / 180);
            _focalDistance = (float)(_orbitDB.Eccentricity * _orbitEllipseMajor * 0.5f); //linear ecentricity




            double x = 0;
            double y = 0;
            _points = new PointD[_numberOfArcSegments + 1];
            double angle = _orbitAngleRadians;
            double incrementAngle = Math.PI * 2 / _numberOfArcSegments;
            for (int i = 0; i < _numberOfArcSegments + 1; i++)
            {
                
                x = _focalDistance + _orbitEllipseSemiMaj * Math.Sin(angle); //we add the focal distance so the focal point is "center"
                y =  _orbitEllipseSemiMinor * Math.Cos(angle);
                angle += incrementAngle;
                _points[i] = new PointD() { x = x, y = y };
            }

            Update();

        }



        public override void Update()
        {
            //update world position
            /*
            _focalX = _parentPositionDB.X;
            _focalY = _parentPositionDB.Y;
            _ctrPosX = _focalX - _focalDistance;
            _ctrPosY = _focalY;
            */

            //calculate the angle to the entity's position
            Vector4 pos = _bodyPositionDB.AbsolutePosition - _parentPositionDB.AbsolutePosition; //adjust so moons get the right positions    
            double normalX = (pos.X * Math.Cos(-_orbitAngleRadians)) - (pos.Y * Math.Sin(-_orbitAngleRadians));
            double normalY = (pos.X * Math.Sin(-_orbitAngleRadians)) + (pos.Y * Math.Cos(-_orbitAngleRadians));
            normalX += _focalDistance; //adjust for focal point
            normalY *= (_orbitEllipseMajor / _orbitEllipseMinor); //adjust for elliptic angle. 

            _ellipseStartArcAngleRadians = (float)Angle.NormaliseRadians((Math.Atan2(normalY, normalX) * 180 / Math.PI));   


            //calculate anything that could have changed fom the user. - we can probilby move this to another function and call it when the ui makes these changes. 
            _segmentArcSweepAngleRadians = (float)Angle.NormaliseRadians((Math.PI * 2.0) / (_numberOfArcSegments));
            _numberOfDrawSegments = (int)(_numberOfArcSegments / _ellipseSweepAngleRadians);
            _alphaChangeAmount = (byte)(255 / _numberOfDrawSegments);


        }




        public override void Draw(IntPtr rendererPtr, Camera camera)
        {
            //Update();
            byte oR, oG, oB, oA;
            SDL.SDL_GetRenderDrawColor(rendererPtr, out oR, out oG, out oB, out oA);
            SDL.SDL_BlendMode blendMode;
            SDL.SDL_GetRenderDrawBlendMode(rendererPtr, out blendMode);
            SDL.SDL_SetRenderDrawBlendMode(rendererPtr, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);

            float zoomLevel = 1;
            int posX = ViewScreenPos.x;
            int posY = ViewScreenPos.y;
            if (ShapesScaleWithZoom)
                zoomLevel = camera.ZoomLevel;

            //do matrix transformations for zoom
            Matrix matrix = new Matrix();
            matrix.Scale(camera.ZoomLevel);

            var translatedPoints = new List<SDL.SDL_Point>();

            //get the indexPosition in the point array we want to start drawing from - this should be where the planet is. 
            int index = (int)(_ellipseStartArcAngleRadians / _segmentArcSweepAngleRadians); 

            var camerapoint = camera.CameraViewCoordinate();
            
            for (int i = 0; i < _numberOfArcSegments; i++)
            {
                if (index < _numberOfArcSegments)
                    index++;
                else
                    index = 0;
                var translated = matrix.Transform(_points[index].x, _points[index].y);

                int x = (int)(ViewScreenPos.x + translated.x + camerapoint.x);// * zoomLevel);
                int y = (int)(ViewScreenPos.y + translated.y + camerapoint.y);// * zoomLevel);
                SDL.SDL_Point point = new SDL.SDL_Point() { x = x, y = y };
                index++;

                translatedPoints.Add(point);
            }




            byte alpha = 255;
            for (int i = 0; i < _numberOfArcSegments - 1; i++)
            {
                SDL.SDL_SetRenderDrawColor(rendererPtr, Colour.r, Colour.g, Colour.b, alpha);
                SDL.SDL_RenderDrawLine(rendererPtr, translatedPoints[i].x, translatedPoints[i].y, translatedPoints[i + 1].x, translatedPoints[i +1].y);
                alpha -= _alphaChangeAmount; 

            }
            SDL.SDL_SetRenderDrawColor(rendererPtr, oR, oG, oB, oA);
            SDL.SDL_SetRenderDrawBlendMode(rendererPtr, blendMode);
        }
    }
}