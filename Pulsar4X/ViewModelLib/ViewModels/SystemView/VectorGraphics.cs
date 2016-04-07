﻿using System;
using System.Collections.Generic;
using Pulsar4X.ECSLib;

namespace Pulsar4X.ViewModel.SystemView
{
    /// <summary>
    /// Base class for system view items. 
    /// </summary>
    public class VectorGraphicDataBase : ViewModelBase
    {

        public List<VectorPathPenPair> PathList { get; set; } = new List<VectorPathPenPair>();

        /// <summary>
        /// system position in AU
        /// </summary>
        public float PosX
        {
            get { return _posx; }
            set { _posx = value; OnPropertyChanged(); }
        }
        private float _posx = 0;
        /// <summary>
        /// systemPosition in AU
        /// </summary>
        public float PosY
        {
            get { return _posy; }
            set { _posy = value; OnPropertyChanged(); }
        }
        private float _posy = 0;


        public float Rotation { get; set; }

        public float Scale
        {
            get { return _scale; }
            set { _scale = value; OnPropertyChanged(); }
        }
        private float _scale = 200;

        /// <summary>
        /// most icons wont change size with zoom, however things like the orbit lines will. 
        /// </summary>
        public bool SizeAffectedbyZoom { get; set; } = false;
    }


    /// <summary>
    /// an Icon, anything shown on the system map that does not scale with the camera zoom should be able to be added to this base clases PathList
    /// </summary>
    public class IconData : VectorGraphicDataBase
    {
        private Entity _bodyEntity;
        private PositionDB PositionBlob { get { return _bodyEntity.GetDataBlob<PositionDB>(); } }

        private DateTime _currentDateTime;
        /// <summary>
        /// setting the CurrentDateTime causes the UpdatePosition() method to run.
        /// </summary>
        public DateTime CurrentDateTime
        {
            get { return _currentDateTime; }
            set { _currentDateTime = value; updatePosition(); }
        }

        /// <summary>
        /// setting the PosX and PosY causes an OnPropertyChanged event. (in base class)
        /// </summary>
        private void updatePosition()
        {
            PosX = (float)PositionBlob.Position.X;
            PosY = (float)PositionBlob.Position.Y;
        }


        /// <summary>
        /// Constructor for IconData
        /// </summary>
        /// <param name="entity">The Object Entity</param>
        public IconData(Entity entity)
        {
            _bodyEntity = entity;

            //TODO: filter/sort what the object is by the datablobs that it has, and run the apropriate method for the type of icon.  
            if (entity.HasDataBlob<SystemBodyDB>())
                PlanetIcon(entity);
            else if (entity.HasDataBlob<StarDB>())
                StarIcon(entity);

        }

        /// <summary>
        /// creates the shape for a fleet icon. 
        /// </summary>
        /// <param name="fleet"></param>
        private void FleetIcon(Entity fleet)
        {
            PenData penData = new PenData();
            penData.Green = 255;
            float width = 6;
            float height = 6;
            updatePosition();

            VectorPathPenPair pathPair = new VectorPathPenPair(penData, new EllipseData(PosX, PosY, width, height));
            PathList.Add(pathPair);
        }


        /// <summary>
        /// creates teh shame for a StarIcon
        /// </summary>
        /// <param name="star"></param>
        private void StarIcon(Entity star)
        {
            //TODO: change pen colour depending on star temp?
            PenData penData = new PenData();
            penData.Red = 100;
            penData.Green = 100;
            penData.Blue = 0;
            float width = 8;
            float height = 8;
            updatePosition();

            float hw = width * 0.25f;
            float hh = height * 0.25f;

            VectorPathPenPair pathPair = new VectorPathPenPair(penData, new RectangleData(PosX, PosY, width, height));
            pathPair.VectorShapes.Add(new BezierData(0, -height, -width, 0, -hw, -hh, -hw, -hh));
            pathPair.VectorShapes.Add(new BezierData(-width, 0, 0, height, -hw, hh, -hw, hh));
            pathPair.VectorShapes.Add(new BezierData(0, height, width, 0, hw, hh, hw, hh));
            pathPair.VectorShapes.Add(new BezierData(width, 0, 0, -height, hw, -hh, hw, -hh));
            PathList.Add(pathPair);
        }


        /// <summary>
        /// creates te shape for a planet icon
        /// </summary>
        /// <param name="planet"></param>
        private void PlanetIcon(Entity planet)
        {
            SystemBodyDB sysBody = planet.GetDataBlob<SystemBodyDB>();
            //TODO tweak shape/add symbol and colour depending on planet type, and other stats. ie: size, temp, colony cost for current faction, etc etc.  
            float width = 6;
            float height = 6;

            switch (sysBody.BodyType)
            {
                case BodyType.Asteroid: //throw some rand in the shape?
                    { }
                    break;
                case BodyType.Comet:
                    { }
                    break;
                case BodyType.DwarfPlanet:
                    { }
                    break;
                case BodyType.GasDwarf:
                    { }
                    break;
                case BodyType.GasGiant:
                    { }
                    break;
                case BodyType.IceGiant:
                    { }
                    break;
                case BodyType.Moon:
                    { }
                    break;
                case BodyType.Terrestrial:
                    {
                        PenData penData = new PenData();
                        penData.Green = 100;
                        penData.Blue = 200;

                        _bodyEntity = planet;
                        updatePosition();
                        VectorPathPenPair pathPair = new VectorPathPenPair(penData, new EllipseData(PosX, PosY, width, height));
                        PathList.Add(pathPair);
                    }
                    break;

                default:
                    {
                        PenData penData = new PenData();
                        penData.Green = 255;
                        _bodyEntity = planet;
                        updatePosition();
                        VectorPathPenPair pathPair = new VectorPathPenPair(penData, new EllipseData(PosX, PosY, width, height));
                        PathList.Add(pathPair);
                    }
                    break;
            }
        }
    }


    /// <summary>
    /// an simple OrbitEllipse
    /// </summary>
    public class OrbitEllipseSimple : VectorGraphicDataBase
    {
        public byte Segments { get; set; } = 255;

        public OrbitDB OrbitDB { get; set; }
        private DateTime _currentDateTime;
        public DateTime CurrentDateTime
        {
            get { return _currentDateTime; }
            set { _currentDateTime = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="orbit"></param>
        public OrbitEllipseSimple(OrbitDB orbit)
        {
            SizeAffectedbyZoom = true;
            OrbitDB = orbit;

            Rotation = 0;
            //Width = 2;
            //Height = 2;
            PosX = 0;
            PosY = 0;

            if (orbit.Parent != null && orbit.Parent.HasDataBlob<PositionDB>())
            {
                PosX = (float)orbit.Parent.GetDataBlob<PositionDB>().X;
                PosY = (float)orbit.Parent.GetDataBlob<PositionDB>().Y;
            }

            // setup date time etc.
            DateTime currTime = DateTime.Now;
            DateTime EndTime = currTime + OrbitDB.OrbitalPeriod;
            TimeSpan stepTime = new TimeSpan((EndTime - currTime).Ticks / 365);
            EndTime -= stepTime; // to end the loop 1 early.

            // get inital positions on orbit
            var startPos = OrbitProcessor.GetPosition(orbit, currTime);
            currTime += stepTime;
            var currPos = OrbitProcessor.GetPosition(orbit, currTime);
            var prevPos = currPos;
            currTime += stepTime;

            // create first line segment.
            PenData pen = new PenData();
            pen.Red = 255;
            pen.Green = 248;
            pen.Blue = 220;
            pen.Thickness = 2.2f;
            LineData line = new LineData((float)startPos.X, (float)startPos.Y, (float)currPos.X, (float)currPos.Y);
            VectorPathPenPair pathPenPair = new VectorPathPenPair(pen, line);
            PathList.Add(pathPenPair);

            // create rest of the lin segments.
            for (; currTime < EndTime; currTime += stepTime)
            {
                currPos = OrbitProcessor.GetPosition(orbit, currTime);

                pen = new PenData();
                pen.Red = 255;
                pen.Green = 248;
                pen.Blue = 220;
                pen.Thickness = 2.2f;
                line = new LineData((float)prevPos.X, (float)prevPos.Y, (float)currPos.X, (float)currPos.Y);
                pathPenPair = new VectorPathPenPair(pen, line);
                PathList.Add(pathPenPair);

                prevPos = currPos;
            }

            // create last line segment, hoking up the ends.
            currPos = OrbitProcessor.GetPosition(orbit, EndTime);
            pen = new PenData();
            pen.Red = 255;
            pen.Green = 248;
            pen.Blue = 220;
            pen.Thickness = 2.2f;
            line = new LineData((float)prevPos.X, (float)prevPos.Y, (float)currPos.X, (float)currPos.Y);
            pathPenPair = new VectorPathPenPair(pen, line);
            PathList.Add(pathPenPair);
        }
    }


    public class OrbitEllipseSimpleFading : VectorGraphicDataBase
    {
        public byte Segments { get; set; } = 255;

        private Entity OrbitEntity { get; set; }

        public OrbitDB OrbitDB
        {
            get { return OrbitEntity.GetDataBlob<OrbitDB>(); }
        }

        public PositionDB ParentPositionDB { get { return OrbitDB.Parent.GetDataBlob<PositionDB>(); } }

        public PositionDB BodyPositionDB { get { return OrbitEntity.GetDataBlob<PositionDB>(); } }


        private DateTime _currentDateTime;
        public DateTime CurrentDateTime
        {
            get { return _currentDateTime; }
            set { _currentDateTime = value; UpdatePosition(); UpdateAlphaFade(); }
        }
        /// <summary>
        /// This is the index that the body is currently at. (or maybe the next one..)
        /// </summary>
        public byte StartIndex { get; set; }

        private TimeSpan StepTime { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="orbit"></param>
        public OrbitEllipseSimpleFading(Entity orbitEntity)
        {
            OrbitEntity = orbitEntity;
            SizeAffectedbyZoom = true;
            //OrbitDB = orbit;
            
            Rotation = 0;
            //Width = 2;
            //Height = 2;
            PosX = 0;
            PosY = 0;

            UpdatePosition();

            // setup date time etc.
            DateTime currTime = OrbitDB.Epoch;
            DateTime EndTime = currTime + OrbitDB.OrbitalPeriod;
            StepTime = new TimeSpan((EndTime - currTime).Ticks / Segments );//365);
            //EndTime -= stepTime; // to end the loop 1 early.

            // get inital positions on orbit
            var startPos = OrbitProcessor.GetPosition(OrbitDB, currTime);
            currTime += StepTime;
            var currPos = OrbitProcessor.GetPosition(OrbitDB, currTime);
            var prevPos = currPos;
            currTime += StepTime;

            //create first line segment.
            PenData pen = new PenData();
            pen.Red = 255;
            pen.Green = 248;
            pen.Blue = 220;
            pen.Thickness = 1f;
            LineData line = new LineData((float)startPos.X, (float)startPos.Y, (float)currPos.X, (float)currPos.Y);
            VectorPathPenPair pathPenPair = new VectorPathPenPair(pen, line);
            PathList.Add(pathPenPair);

            // create rest of the lin segments.
            for (; currTime < EndTime; currTime += StepTime)
            {
                currPos = OrbitProcessor.GetPosition(OrbitDB, currTime);
                pen = new PenData();
                pen = new PenData();
                pen.Red = 255;
                pen.Green = 248;
                pen.Blue = 220;
                pen.Thickness = 1f;
                line = new LineData((float)prevPos.X, (float)prevPos.Y, (float)currPos.X, (float)currPos.Y);
                pathPenPair = new VectorPathPenPair(pen, line);
                PathList.Add(pathPenPair);

                prevPos = currPos;
            }

            // create last line segment, hooking up the ends.
            currPos = OrbitProcessor.GetPosition(OrbitDB, EndTime);
            pen = new PenData();
            pen.Red = 255;
            pen.Green = 248;
            pen.Blue = 220;
            pen.Thickness = 1f;
            line = new LineData((float)prevPos.X, (float)prevPos.Y, (float)currPos.X, (float)currPos.Y);
            pathPenPair = new VectorPathPenPair(pen, line);
            PathList.Add(pathPenPair);

            CurrentDateTime = OrbitDB.Epoch;
        }



        /// <summary>
        /// Updates the position, is fired when CurrentDateTime is set. (required for a moons orbit for example)
        /// </summary>
        private void UpdatePosition()
        {
            if (OrbitDB.Parent != null && OrbitDB.Parent.HasDataBlob<OrbitDB>())
            {
                PosX = (float)ParentPositionDB.Position.X ;
                PosY = (float)ParentPositionDB.Position.Y ;
            }
        }

        /// <summary>
        /// updates the pen to get a fading line on the orbit. 
        /// </summary>
        public void UpdateAlphaFade()
        {
            SetStartPos();
            byte i = 0;
            foreach (var item in PathList)
            {
                item.Pen.Alpha = (byte)(255 - StartIndex + i);
                i++;
            }
        }

        /// <summary>
        /// finds the angle to the current position and sets the startIndex to the apropriate number. the index should now represent which line the body is at. 
        /// </summary>
        public void SetStartPos()
        {
            TimeSpan timeSinceEpoch = CurrentDateTime - OrbitDB.Epoch;
            StartIndex = (byte)((timeSinceEpoch.Ticks / StepTime.Ticks) -1);
        }
    }

    /// <summary>
    /// generic data for drawing an OrbitEllipse which fades towards the tail
    /// </summary>
    public class OrbitEllipseFading : VectorGraphicDataBase
    {
        /// <summary>
        /// number of segments in the orbit, this is mostly for an increasing alpha chan.
        /// </summary>
        public byte Segments { get; set; } = 255;
        /// <summary>
        /// each of the arcs are stored here
        /// </summary>
        //public List<ArcData> ArcList { get; } = new List<ArcData>();
        /// <summary>
        /// This is the index that the body is currently at. (or maybe the next one..)
        /// </summary>
        public byte StartIndex { get; set; }

        public OrbitDB OrbitDB { get; set; }
        public PositionDB PositionDB { get; set; }
        private DateTime _currentDateTime;
        public DateTime CurrentDateTime
        {
            get { return _currentDateTime; }
            set { _currentDateTime = value; updatePosition(); updateAlphaFade(); }
        }

        private float _width;
        private float _height;
        private float _focalPoint;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="orbit"></param>
        public OrbitEllipseFading(OrbitDB orbit, PositionDB positionDB)
        {
            //TODO:May have to create a smaller arc for the first segment, and full alpha the segment the body is at.
            Rotation = (float)(orbit.LongitudeOfAscendingNode + orbit.ArgumentOfPeriapsis) + 180; //TODO adjust for 3d orbits. ie if the orbit has an Z axis, this is likely to be wrong. 
            _width = (float)orbit.SemiMajorAxis * 2; //Major Axis
            _height = (float)Math.Sqrt(((orbit.SemiMajorAxis * Math.Sqrt(1 - orbit.Eccentricity * orbit.Eccentricity)) * orbit.SemiMajorAxis * (1 - orbit.Eccentricity * orbit.Eccentricity))) * 2;   //minor Axis
            _focalPoint = (float)Math.Sqrt(_width * _width * 0.5f - _height * _height * 0.5f);
            SizeAffectedbyZoom = true;
            OrbitDB = orbit;
            PositionDB = positionDB;
            updatePosition();

            float start = 0;
            float sweep = 360.0f / Segments;
            for (int i = 0; i < Segments; i++)
            {
                PenData pen = new PenData();
                pen.Red = 255;
                pen.Green = 248;
                pen.Blue = 220;
                ArcData arc = new ArcData(PosX, PosY, _width, _height, start, sweep);
                VectorPathPenPair pathPenPair = new VectorPathPenPair(pen, arc);
                PathList.Add(pathPenPair);
                start += sweep;
            }
            updateAlphaFade();
        }

        public void SetStartPos()
        {
            float angle = (float)(OrbitDB.LongitudeOfAscendingNode + OrbitDB.ArgumentOfPeriapsis + OrbitProcessor.GetTrueAnomaly(OrbitDB, _currentDateTime)) + Rotation;
            float trueAnomaly = (float)OrbitProcessor.GetTrueAnomaly(OrbitDB, _currentDateTime);
            Vector4 position = OrbitProcessor.GetPosition(OrbitDB, CurrentDateTime);
            float angle2 = (float)(Math.Atan2(position.Y, position.X) * 180 / Math.PI);

            float degreesPerSegment = 360 / (Convert.ToSingle(Segments));
            StartIndex = (byte)(angle2 / degreesPerSegment);

        }

        private void updatePosition()
        {
            if (OrbitDB.Parent != null && OrbitDB.Parent.HasDataBlob<OrbitDB>())
            {
                PosX = (float)PositionDB.Position.X - _width * 0.5f + _focalPoint;
                PosY = (float)PositionDB.Position.Y - _width * 0.5f;
            }
        }

        public void updateAlphaFade()
        {
            SetStartPos();
            byte i = 0;
            foreach (var item in PathList)
            {
                item.Pen.Alpha = (byte)(255 - StartIndex + i);
                i++;
            }
        }
    }


    /// <summary>
    /// a list of vector shapes with a pen
    /// </summary>
    public class VectorPathPenPair
    {
        public PenData Pen { get; set; } = new PenData();
        public List<VectorShapeBase> VectorShapes { get; set; } = new List<VectorShapeBase>();
        public VectorPathPenPair(VectorShapeBase shape)
        {
            VectorShapes.Add(shape);
        }

        public VectorPathPenPair(PenData pen, VectorShapeBase shape) : this(shape)
        { Pen = pen; }

        public VectorPathPenPair(PenData pen, List<VectorShapeBase> shapes)
        {
            Pen = pen;
            foreach (var shape in shapes)
            {
                VectorShapes.Add(shape);
            }
        }
    }

}
