using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Reflection;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Urho;
using Urho.Urho2D;

namespace URHO2D.Template
{
    public struct SVGPathPoint
    {
        public string CommandType;
        public Vector2 vPoint;
        public List<Vector2> vPoints;
        public Dictionary<string, float> ShapeDetails;
        public TerrainType terrainType;
        public List<SVGPathPoint> rotatorPoints;
        public Vector2 rotatorSize;
    };

	public sealed class TerrainType
	{
		readonly String name;
		readonly String value;

		public static readonly TerrainType GENERAL = new TerrainType("333333", "General");
		public static readonly TerrainType ROTATING = new TerrainType("FF8000", "Rotating");
		public static readonly TerrainType UNDEFINED = new TerrainType("000000", "UNDEFINED");

		static readonly Dictionary<string, TerrainType> instance = new Dictionary<string, TerrainType>();

		TerrainType(String value, String name)
		{
			this.name = name;
			this.value = value;
		}
		public TerrainType(String value)
		{
			switch (value)
			{
				case "333333":
					this.value = value;
					name = "General";
					break;
				case "FF8000":
					this.value = value;
					name = "Rotating";
					break;
				default:
					this.value = "000000";
					name = "UNDEFINED";
					break;
			}
		}
		public override String ToString()
		{
			return name;
		}
		public static explicit operator TerrainType(string str)
		{
			if (instance.TryGetValue(str, out TerrainType result))
				return result;
			throw new InvalidCastException();
		}
		public override bool Equals(object obj)
		{
			var item = obj as TerrainType;

			if (item == null)
			{
				return false;
			}

			return value.Equals(item.value);
		}
		public override int GetHashCode()
		{
			return this.value.GetHashCode();
		}
	}

    public class SVGDataDict : Dictionary<string, dynamic>
    {
        public SVGDataDict()
        {
        }
        public SVGDataDict(string file)
        {
            LoadSVG(file);
        }
        public void LoadSVG(string file)
        {
            Clear();
            var assembly = typeof(PList).GetTypeInfo().Assembly;
            Stream stream = assembly.GetManifestResourceStream("URHO2D.Template." + file);

            XDocument doc = XDocument.Load(stream);
            doc.StripNamespace();
            XElement svg = doc.Element("svg");
            float[] viewBoxDets = svg.Attribute("viewBox").Value.Split(new char[] { ' ' }).Select(float.Parse).ToArray();
            var dictElements = svg.Elements();
            ParseSVG(this, dictElements, viewBoxDets[2] * Application.PixelSize, viewBoxDets[2] * Application.PixelSize);
        }
        Vector2 CenterOfRotator(List<SVGPathPoint> points) {
            //List<Vector2> dots = new List<Point>();
            float totalX = 0, totalY = 0;
			foreach (SVGPathPoint p in points)
			{
                totalX += p.vPoint.X;
				totalY += p.vPoint.Y;
                //maxX = Math.Max(VectorDistance(), maxX);
			}
            float centerX = totalX / points.Count;
			float centerY = totalY / points.Count;
            return new Vector2(centerX, centerY);
		}
        float VectorDistance(List<SVGPathPoint> points, bool getX) {
            var pointWithIndex = points.Select((x, i) => new { Point = x, Index = i });
			var pointPairs =
					from p1 in pointWithIndex
					from p2 in pointWithIndex
					where p1.Index > p2.Index
					select new { p1 = p1.Point, p2 = p2.Point };
			if (getX)
			{
                var distances = pointPairs.Select(x => EuclideanX(x.p1.vPoint, x.p2.vPoint)).ToList();
                return (float)distances.Max();
			}
			else
			{
                var distances = pointPairs.Select(x => EuclideanY(x.p1.vPoint, x.p2.vPoint)).ToList();
                return (float)distances.Max();
			}
        }
		public static double EuclideanX(Vector2 p1, Vector2 p2)
		{
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2));
		}
		public static double EuclideanY(Vector2 p1, Vector2 p2)
		{
			return Math.Sqrt(Math.Pow(p1.Y - p2.Y, 2));
		}
        TerrainType GetTerrainType(XElement key) {
            var hex = "no fill";
			var stylemap = key.Attribute("style")?.Value
			.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
			.Select(x => x.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries))
			.ToDictionary(p => p[0], p => p[1]);
			hex = RgbToHex(stylemap["fill"]);
            return new TerrainType(hex);
        }
		private void ParseSVG(SVGDataDict dict, IEnumerable<XElement> elements, float w, float docSizeH)
		{
			List<SVGPathPoint> svgTokens = new List<SVGPathPoint>();
			List<SVGPathPoint> rotPoints = null;
			for (int i = 0; i < elements.Count(); i++)
			{
				XElement key = elements.ElementAt(i);

                TerrainType terType = GetTerrainType(key);

				var isRotator = terType.Equals(TerrainType.ROTATING);

                rotPoints = isRotator ? new List<SVGPathPoint>() : null;

				//Debug.WriteLine($"current key: {key.Name.LocalName} svgTokens count: {svgTokens.Count} TerrainType: {terType.ToString()}");

				try
				{
					if (terType.Equals(TerrainType.ROTATING))
					{
						Debug.WriteLine("TerrainType.ROTATING  need to be grouped in vPoints");
					}
					else
					{
						Debug.WriteLine($"terraintype: {terType?.ToString()}");
					}

					if (key.Name.LocalName == "path")
					{
						var path = key.Attribute("d").Value;

						string separators = @"(?=[A-Za-z])";
						var tokens = Regex.Split(path, separators).Where(t => !string.IsNullOrEmpty(t));

						var currentPosition = Vector2.Zero;
						var relative = false;
						Vector2 control1 = Vector2.Zero;
						Vector2 control2 = Vector2.Zero;
						Vector2 control3 = Vector2.Zero;
						Vector2 prevControl = Vector2.Zero;
						var prevCommand = string.Empty;



						//Debug.WriteLine($"parsing: {tokens}");
						//foreach (string token in tokens)
                        for (int t = 0; t < tokens.Count(); t++)
						{
                            string token = tokens.ElementAt(t);
							// M = moveto
							// L = lineto
							// H = horizontal lineto
							// V = vertical lineto
							// C = curveto
							// S = smooth curveto
							// Q = quadratic Bézier curve
							// T = smooth quadratic Bézier curveto
							// A = elliptical Arc
							// Z = closepath


							//Debug.WriteLine($"parsing: {token}");

							SVGPathPoint p = new SVGPathPoint();
							var command = token.Substring(0, 1);
							p.CommandType = command;
							p.terrainType = terType;
							//Debug.WriteLine($"parsing: {command}");

							var pointStr = token.Substring(1);
							//var pointDets = pointStr.Split(',');

							float[] pointDets = new float[0];

							if (command.ToLower() == "c")
							{
								// (    x1         y1      x2 y     2       x     y)
								// C  3424.92,  980.017 507.315,-197.292 81.479,243.3

								var ccc = pointStr.Split(new char[] { ',', ' ' });
								var bb = ccc;

								if (pointStr.Length > 0)
								{
									pointDets = pointStr.Split(new char[] { ',', ' ' }).Select(float.Parse).ToArray();
								}
							}
							else
							{
								if (pointStr.Length > 0)
								{
									pointDets = pointStr.Split(',').Select(float.Parse).ToArray();
								}
							}

							//pointDets = pointStr.Split(new char[] { ',', ' ' }).Select(float.Parse).ToArray();

							//pointDets.Aggregate();
							pointDets = pointDets.Select(r => r * Application.PixelSize).ToArray();


							switch (p.CommandType)
							{
								case "Z":
								case "z":
									// closepath: If a path is incomplete, finish it, close the loop,
									//            and add it to the Box2D world. 
									//if (chainDef.vertices.length >= 3)
									//{
									//  chainDef.vertices.pop(); // the last vertex of the loop is redundant.
									//  chainDef.vertexCount = chainDef.vertices.length;
									//  chainDef.isALoop = true;
									//  world.GetGroundBody().CreateShape(chainDef);
									//}
									//chainDef.vertices.length = 0;
									//chainDef.vertices.push(currentPosition);
									//Debug.WriteLine("not sure if paths need closing in urho ??");


									p.vPoint = currentPosition;
									break;
								case "M":
								case "m":
									// moveto: If a path is incomplete, finish it without closing the loop
									//          and add it to the Box2D world.
									//          Start a new path.
									//if (chainDef.vertices.length >= 2)
									//{
									//  chainDef.vertexCount = chainDef.vertices.length;
									//  chainDef.isALoop = false;
									//  world.GetGroundBody().CreateShape(chainDef);
									//}
									relative = (command == "m");
									if (relative)
									{
										currentPosition = new Vector2(currentPosition.X + pointDets[0],
																	  currentPosition.Y + (docSizeH - pointDets[1]));
									}
									else
									{
										currentPosition = new Vector2(pointDets[0], docSizeH - pointDets[1]);
									}
									p.vPoint = currentPosition;
									//i += 2;
									//chainDef.vertices.length = 0;
									//chainDef.vertices.push(currentPosition);
									break;
								// According to the SVG spec, a moveto command can be implicitly followed
								// by lineto coordinates. So there is no "break" here.
								case "L":
								case "l":
									// lineto: a series of straight lines. Keep parsing until you hit a non-number. 
									if (command == "l") relative = true;
									else if (command == "L") relative = false;

									for (int li = 0; li < pointDets.Length; li++)
									{
										float pval = pointDets[li];
										//foreach (float pval in pointDets) {
										if (relative)
										{
											currentPosition = new Vector2(currentPosition.X + pval,
																		  currentPosition.Y + (docSizeH - pointDets[li + 1]));
										}
										else
										{
											currentPosition = new Vector2(pointDets[li], (docSizeH - pointDets[li + 1]));
										}
										li += 1;
									}
									p.vPoint = currentPosition;
									break;
								case "H":
								case "h":
									// horizontal lineto: a series of horizontal lines. 
									//                    keep parsing until you hit a non-number. 
									//                    Box2D works much better if adjacent parallel lines
									//                    are merged into one, so I'll go ahead and merge them.
									relative = (command == "h");
									for (int li = 0; li < pointDets.Length; li++)
									{
										if (relative)
										{
											currentPosition = new Vector2(currentPosition.X + pointDets[li],
																		 currentPosition.Y);
										}
										else
										{
											currentPosition = new Vector2(pointDets[li], currentPosition.Y);
										}
										//li++;
									} //while (float.TryParse(pointDets[i],outf));
									p.vPoint = currentPosition;
									//chainDef.vertices.push(currentPosition);
									break;
								case "V":
								case "v":
									// vertical lineto: a series of vertical lines. 
									//                  keep parsing until you hit a non-number. 
									//                  Box2D works much better if adjacent parallel lines
									//                  are merged into one, so I'll go ahead and merge them.
									relative = (command == "v");
									for (int li = 0; li < pointDets.Length; li++)
									{
										if (relative)
										{
											currentPosition = new Vector2(currentPosition.X,
																		  currentPosition.Y + (docSizeH - pointDets[li]));
										}
										else
										{
											currentPosition = new Vector2(currentPosition.X, docSizeH - pointDets[li]);
										}
										//li++;
									} // while (!isNaN(parseFloat(pointDets[i])));
									  //chainDef.vertices.push(currentPosition);
									p.vPoint = currentPosition;
									break;
								case "C":
								case "c":
									{
										// curveto
										relative = (command == "c");
										if (relative)
										{
											control1 = new Vector2(currentPosition.X + pointDets[0], currentPosition.Y + (docSizeH - pointDets[1]));
											control2 = new Vector2(currentPosition.X + pointDets[2], currentPosition.Y + (docSizeH - pointDets[3]));
											control3 = new Vector2(currentPosition.X + pointDets[4], currentPosition.Y + (docSizeH - pointDets[5]));
										}
										else
										{
											control1 = new Vector2(pointDets[0], docSizeH - pointDets[1]);
											control2 = new Vector2(pointDets[2], docSizeH - pointDets[3]);
											control3 = new Vector2(pointDets[4], docSizeH - pointDets[5]);
										}


										List<Vector2> bzc = new List<Vector2>();
										bzc.Add(currentPosition);
										bzc.Add(control1);
										bzc.Add(control2);
										bzc.Add(control3);

										var c = Calculate_bezier(bzc);
										currentPosition = c.Last();

										p.vPoints = c;
									}
									break;
								case "S":
								case "s":
									{
										// shorthand curveto
										relative = (command == "s");
										if (prevCommand == "C" || prevCommand == "c" || prevCommand == "S" || prevCommand == "s")
										{
											control1 = new Vector2(currentPosition.X * 2 - prevControl.X,
																  currentPosition.Y * 2 - prevControl.Y);
										}
										else
										{
											control1 = new Vector2(currentPosition.X,
																  currentPosition.Y);
										}
										if (relative)
										{
											control2 = new Vector2(currentPosition.X + pointDets[0],
																  currentPosition.Y + (docSizeH - pointDets[1]));
											control3 = new Vector2(currentPosition.X + pointDets[2],
																  currentPosition.Y + (docSizeH - pointDets[3]));
										}
										else
										{
											control2 = new Vector2(pointDets[0], docSizeH - pointDets[1]);
											control3 = new Vector2(pointDets[2], docSizeH - pointDets[3]);
										}
										List<Vector2> bzc = new List<Vector2>();
										bzc.Add(currentPosition);
										bzc.Add(control1);
										bzc.Add(control2);
										bzc.Add(control3);

										var c = Calculate_bezier(bzc);
										currentPosition = c.Last();
										p.vPoints = c;
									}
									break;
								case "Q":
								case "q":
								case "T":
								case "t":
								case "A":
								case "a":
									Debug.WriteLine("TODO: Unimplemented path command: " + command);
									break;
							}
							if (isRotator)
							{
								rotPoints.Add(p);

                                if ((t + 1) == tokens.Count())
								{
									SVGPathPoint rp = new SVGPathPoint();
									rp.CommandType = "Rotator";
									rp.rotatorPoints = rotPoints;
									rp.vPoint = CenterOfRotator(rotPoints);
									rp.rotatorSize = new Vector2(VectorDistance(rotPoints, true) * 0.5f, VectorDistance(rotPoints, false) * 0.5f);

									svgTokens.Add(rp);
									rotPoints = null;
								}
							}
							else
							{
								svgTokens.Add(p);
							}



						}
					}
					else if (key.Name.LocalName == "rect")
					{
						var X = float.Parse(key.Attribute("x").Value) * Application.PixelSize;
						var Y = docSizeH - float.Parse(key.Attribute("y").Value) * Application.PixelSize;
						var width = float.Parse(key.Attribute("width").Value) * Application.PixelSize;
						var height = float.Parse(key.Attribute("height").Value) * Application.PixelSize;
						SVGPathPoint p = new SVGPathPoint();
						p.CommandType = "rect";
						Dictionary<string, float> boxDets = new Dictionary<string, float>();
						boxDets["X"] = (X + (width * 0.5f));
						boxDets["Y"] = (Y - (height * 0.5f));
						boxDets["width"] = width;
						boxDets["height"] = height;
						p.ShapeDetails = boxDets;
						svgTokens.Add(p);
						//Debug.WriteLine($"svgTokens add rect: {X}, {Y}, {width}, {height} : svgTokens count: {svgTokens.Count}");
					}
					else if (key.Name.LocalName == "ellipse")
					{
						var X = float.Parse(key.Attribute("cx").Value) * Application.PixelSize;
						var Y = docSizeH - float.Parse(key.Attribute("cy").Value) * Application.PixelSize;
						//  should do something smart if width & height are different
						var width = float.Parse(key.Attribute("rx").Value) * Application.PixelSize;
						var height = float.Parse(key.Attribute("ry").Value) * Application.PixelSize;
						SVGPathPoint p = new SVGPathPoint();
						p.CommandType = "ellipse";
						Dictionary<string, float> ellipseDets = new Dictionary<string, float>();
						ellipseDets["X"] = X; //(X + (width * 0.5f));
						ellipseDets["Y"] = Y; //(Y - (height * 0.5f));
						ellipseDets["width"] = width;
						ellipseDets["height"] = height;
						p.ShapeDetails = ellipseDets;
						svgTokens.Add(p);
						//Debug.WriteLine($"svgTokens add ellipse: {X}, {Y}, {width}, {height} : svgTokens count: {svgTokens.Count}");
					}
					else if (key.Name.LocalName == "circle")
					{
						var X = float.Parse(key.Attribute("cx").Value) * Application.PixelSize;
						var Y = docSizeH - float.Parse(key.Attribute("cy").Value) * Application.PixelSize;
						var rad = float.Parse(key.Attribute("r").Value) * Application.PixelSize;
						SVGPathPoint p = new SVGPathPoint();
						p.CommandType = "circle";
						Dictionary<string, float> ellipseDets = new Dictionary<string, float>();
						ellipseDets["X"] = X; // (X + (rad * 0.5f));
						ellipseDets["Y"] = Y; // (Y + (rad * 0.5f));
						ellipseDets["radius"] = rad;
						p.ShapeDetails = ellipseDets;
						svgTokens.Add(p);
						//Debug.WriteLine($"svgTokens add ellipse: {X}, {Y}, {rad} : svgTokens count: {svgTokens.Count}");
					}
					else if (key.Name.LocalName == "g")
					{
						SVGDataDict plist = new SVGDataDict();
						var gElements = key.Elements();
						ParseSVG(plist, key.Elements(), w, docSizeH);
						List<SVGPathPoint> svgTokens2 = plist["svgTokens"];
						svgTokens = svgTokens.Concat(svgTokens2).ToList();
					}
					else
					{
						Debug.WriteLine($"TODO: Unimplemented Key Name: {key.Name}");
					}
				}
				catch (Exception e)
				{
					var ee = e.Message;
				}
			}
			dict["svgTokens"] = svgTokens;
		}
     //   private void ParseSVG(SVGDataDict dict, IEnumerable<XElement> elements, float w, float docSizeH)
     //   {
     //       List<SVGPathPoint> svgTokens = new List<SVGPathPoint>();
     //       List<SVGPathPoint> rotPoints = null;

     //       for (int i = 0; i < elements.Count(); i++)
     //       {
     //           XElement key = elements.ElementAt(i);

     //           var hex = "no fill";
     //           var stylemap = key.Attribute("style")?.Value
     //               .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
     //               .Select(x => x.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries))
     //               .ToDictionary(p => p[0], p => p[1]);
     //           hex = RgbToHex(stylemap["fill"]);
     //           TerrainType terType = new TerrainType(hex);
     //           var isRotator = terType.Equals(TerrainType.ROTATING);

     //           if (rotPoints != null)
     //           {
     //               if (!isRotator)
     //               {
     //                   SVGPathPoint p = new SVGPathPoint();
     //                   p.CommandType = "Rotator";
     //                   p.rotatorPoints = rotPoints;
     //                   p.vPoint = CenterOfRotator(rotPoints);
     //                   p.rotatorSize = new Vector2(VectorDistance(rotPoints, true) * 0.5f, VectorDistance(rotPoints, false) * 0.5f);

     //                   svgTokens.Add(p);
     //                   rotPoints = null;

     //               }
     //           }
     //           else
     //           {
     //               rotPoints = isRotator ? new List<SVGPathPoint>() : null;
     //           }
     //       }

     //       for (int i = 0; i < elements.Count(); i++)
     //       {
     //           XElement key = elements.ElementAt(i);

     //           //Debug.WriteLine($"current key: {key.Name.LocalName} svgTokens count: {svgTokens.Count} TerrainType: {terType.ToString()}");

     //           try
     //           {
					//if (terType.Equals(TerrainType.ROTATING))
					//{
					//	Debug.WriteLine("TerrainType.ROTATING  need to be grouped in vPoints");
					//}
					//else
					//{
					//	Debug.WriteLine($"terraintype: {terType?.ToString()}");
					//}

       //             if (key.Name.LocalName == "path")
       //             {
       //                 var path = key.Attribute("d").Value;

       //                 string separators = @"(?=[A-Za-z])";
       //                 var tokens = Regex.Split(path, separators).Where(t => !string.IsNullOrEmpty(t));

       //                 var currentPosition = Vector2.Zero;
       //                 var relative = false;
       //                 Vector2 control1 = Vector2.Zero;
       //                 Vector2 control2 = Vector2.Zero;
       //                 Vector2 control3 = Vector2.Zero;
       //                 Vector2 prevControl = Vector2.Zero;
       //                 var prevCommand = string.Empty;



       //                 //Debug.WriteLine($"parsing: {tokens}");
       //                 foreach (string token in tokens)
       //                 {
       //                     // M = moveto
       //                     // L = lineto
       //                     // H = horizontal lineto
       //                     // V = vertical lineto
       //                     // C = curveto
       //                     // S = smooth curveto
       //                     // Q = quadratic Bézier curve
       //                     // T = smooth quadratic Bézier curveto
       //                     // A = elliptical Arc
       //                     // Z = closepath


       //                     //Debug.WriteLine($"parsing: {token}");

       //                     SVGPathPoint p = new SVGPathPoint();
       //                     var command = token.Substring(0, 1);
       //                     p.CommandType = command;
       //                     //Debug.WriteLine($"parsing: {command}");

       //                     var pointStr = token.Substring(1);
       //                     //var pointDets = pointStr.Split(',');

       //                     float[] pointDets = new float[0];

       //                     if (command.ToLower() == "c")
       //                     {
       //                         // (    x1         y1      x2 y     2       x     y)
       //                         // C  3424.92,  980.017 507.315,-197.292 81.479,243.3

       //                         var ccc = pointStr.Split(new char[] { ',', ' ' });
       //                         var bb = ccc;

       //                         if (pointStr.Length > 0)
       //                         {
       //                             pointDets = pointStr.Split(new char[] { ',', ' ' }).Select(float.Parse).ToArray();
       //                         }
       //                     }
       //                     else
       //                     {
       //                         if (pointStr.Length > 0)
       //                         {
       //                             pointDets = pointStr.Split(',').Select(float.Parse).ToArray();
       //                         }
       //                     }

       //                     //pointDets = pointStr.Split(new char[] { ',', ' ' }).Select(float.Parse).ToArray();

       //                     //pointDets.Aggregate();
       //                     pointDets = pointDets.Select(r => r * Application.PixelSize).ToArray();


       //                     switch (p.CommandType)
       //                     {
       //                         case "Z":
       //                         case "z":
       //                             // closepath: If a path is incomplete, finish it, close the loop,
       //                             //            and add it to the Box2D world. 
       //                             //if (chainDef.vertices.length >= 3)
       //                             //{
       //                             //  chainDef.vertices.pop(); // the last vertex of the loop is redundant.
       //                             //  chainDef.vertexCount = chainDef.vertices.length;
       //                             //  chainDef.isALoop = true;
       //                             //  world.GetGroundBody().CreateShape(chainDef);
       //                             //}
       //                             //chainDef.vertices.length = 0;
       //                             //chainDef.vertices.push(currentPosition);
       //                             //Debug.WriteLine("not sure if paths need closing in urho ??");


       //                             p.vPoint = currentPosition;
       //                             break;
       //                         case "M":
       //                         case "m":
       //                             // moveto: If a path is incomplete, finish it without closing the loop
       //                             //          and add it to the Box2D world.
       //                             //          Start a new path.
       //                             //if (chainDef.vertices.length >= 2)
       //                             //{
       //                             //  chainDef.vertexCount = chainDef.vertices.length;
       //                             //  chainDef.isALoop = false;
       //                             //  world.GetGroundBody().CreateShape(chainDef);
       //                             //}
       //                             relative = (command == "m");
       //                             if (relative)
       //                             {
       //                                 currentPosition = new Vector2(currentPosition.X + pointDets[0],
       //                                                               currentPosition.Y + (docSizeH - pointDets[1]));
       //                             }
       //                             else
       //                             {
       //                                 currentPosition = new Vector2(pointDets[0], docSizeH - pointDets[1]);
       //                             }
       //                             p.vPoint = currentPosition;
       //                             //i += 2;
       //                             //chainDef.vertices.length = 0;
       //                             //chainDef.vertices.push(currentPosition);
       //                             break;
       //                         // According to the SVG spec, a moveto command can be implicitly followed
       //                         // by lineto coordinates. So there is no "break" here.
       //                         case "L":
       //                         case "l":
       //                             // lineto: a series of straight lines. Keep parsing until you hit a non-number. 
       //                             if (command == "l") relative = true;
       //                             else if (command == "L") relative = false;

       //                             for (int li = 0; li < pointDets.Length; li++)
       //                             {
       //                                 float pval = pointDets[li];
       //                                 //foreach (float pval in pointDets) {
       //                                 if (relative)
       //                                 {
       //                                     currentPosition = new Vector2(currentPosition.X + pval,
       //                                                                   currentPosition.Y + (docSizeH - pointDets[li + 1]));
       //                                 }
       //                                 else
       //                                 {
       //                                     currentPosition = new Vector2(pointDets[li], (docSizeH - pointDets[li + 1]));
       //                                 }
       //                                 li += 1;
       //                             }
       //                             p.vPoint = currentPosition;
       //                             break;
       //                         case "H":
       //                         case "h":
       //                             // horizontal lineto: a series of horizontal lines. 
       //                             //                    keep parsing until you hit a non-number. 
       //                             //                    Box2D works much better if adjacent parallel lines
       //                             //                    are merged into one, so I'll go ahead and merge them.
       //                             relative = (command == "h");
       //                             for (int li = 0; li < pointDets.Length; li++)
       //                             {
       //                                 if (relative)
       //                                 {
       //                                     currentPosition = new Vector2(currentPosition.X + pointDets[li],
       //                                                                  currentPosition.Y);
       //                                 }
       //                                 else
       //                                 {
       //                                     currentPosition = new Vector2(pointDets[li], currentPosition.Y);
       //                                 }
       //                                 //li++;
       //                             } //while (float.TryParse(pointDets[i],outf));
       //                             p.vPoint = currentPosition;
       //                             //chainDef.vertices.push(currentPosition);
       //                             break;
       //                         case "V":
       //                         case "v":
       //                             // vertical lineto: a series of vertical lines. 
       //                             //                  keep parsing until you hit a non-number. 
       //                             //                  Box2D works much better if adjacent parallel lines
       //                             //                  are merged into one, so I'll go ahead and merge them.
       //                             relative = (command == "v");
       //                             for (int li = 0; li < pointDets.Length; li++)
       //                             {
       //                                 if (relative)
       //                                 {
       //                                     currentPosition = new Vector2(currentPosition.X,
       //                                                                   currentPosition.Y + (docSizeH - pointDets[li]));
       //                                 }
       //                                 else
       //                                 {
       //                                     currentPosition = new Vector2(currentPosition.X, docSizeH - pointDets[li]);
       //                                 }
       //                                 //li++;
       //                             } // while (!isNaN(parseFloat(pointDets[i])));
       //                               //chainDef.vertices.push(currentPosition);
       //                             p.vPoint = currentPosition;
       //                             break;
       //                         case "C":
       //                         case "c":
       //                             {
       //                                 // curveto
       //                                 relative = (command == "c");
       //                                 if (relative)
       //                                 {
       //                                     control1 = new Vector2(currentPosition.X + pointDets[0], currentPosition.Y + (docSizeH - pointDets[1]));
       //                                     control2 = new Vector2(currentPosition.X + pointDets[2], currentPosition.Y + (docSizeH - pointDets[3]));
       //                                     control3 = new Vector2(currentPosition.X + pointDets[4], currentPosition.Y + (docSizeH - pointDets[5]));
       //                                 }
       //                                 else
       //                                 {
       //                                     control1 = new Vector2(pointDets[0], docSizeH - pointDets[1]);
       //                                     control2 = new Vector2(pointDets[2], docSizeH - pointDets[3]);
       //                                     control3 = new Vector2(pointDets[4], docSizeH - pointDets[5]);
       //                                 }


       //                                 List<Vector2> bzc = new List<Vector2>();
       //                                 bzc.Add(currentPosition);
       //                                 bzc.Add(control1);
       //                                 bzc.Add(control2);
       //                                 bzc.Add(control3);

       //                                 var c = Calculate_bezier(bzc);
       //                                 currentPosition = c.Last();

       //                                 p.vPoints = c;
       //                             }
       //                             break;
       //                         case "S":
       //                         case "s":
       //                             {
       //                                 // shorthand curveto
       //                                 relative = (command == "s");
       //                                 if (prevCommand == "C" || prevCommand == "c" || prevCommand == "S" || prevCommand == "s")
       //                                 {
       //                                     control1 = new Vector2(currentPosition.X * 2 - prevControl.X,
       //                                                           currentPosition.Y * 2 - prevControl.Y);
       //                                 }
       //                                 else
       //                                 {
       //                                     control1 = new Vector2(currentPosition.X,
       //                                                           currentPosition.Y);
       //                                 }
       //                                 if (relative)
       //                                 {
       //                                     control2 = new Vector2(currentPosition.X + pointDets[0],
       //                                                           currentPosition.Y + (docSizeH - pointDets[1]));
       //                                     control3 = new Vector2(currentPosition.X + pointDets[2],
       //                                                           currentPosition.Y + (docSizeH - pointDets[3]));
       //                                 }
       //                                 else
       //                                 {
       //                                     control2 = new Vector2(pointDets[0], docSizeH - pointDets[1]);
       //                                     control3 = new Vector2(pointDets[2], docSizeH - pointDets[3]);
       //                                 }
       //                                 List<Vector2> bzc = new List<Vector2>();
							//			bzc.Add(currentPosition);
							//			bzc.Add(control1);
							//			bzc.Add(control2);
							//			bzc.Add(control3);

       //                                 var c = Calculate_bezier(bzc);
       //                                 currentPosition = c.Last();
       //                                 p.vPoints = c;
       //                             }
       //                             break;
       //                         case "Q":
       //                         case "q":
       //                         case "T":
       //                         case "t":
       //                         case "A":
       //                         case "a":
       //                             Debug.WriteLine("TODO: Unimplemented path command: " + command);
       //                             break;
       //                     }
							//if (isRotator)
							//{
							//	rotPoints.Add(p);
							//}
							//else
							//{
							//	svgTokens.Add(p);
							//}
        //                }
        //            }
        //            else if (key.Name.LocalName == "rect")
        //            {
        //                var X = float.Parse(key.Attribute("x").Value) * Application.PixelSize;
        //                var Y = docSizeH - float.Parse(key.Attribute("y").Value) * Application.PixelSize;
        //                var width = float.Parse(key.Attribute("width").Value) * Application.PixelSize;
        //                var height = float.Parse(key.Attribute("height").Value) * Application.PixelSize;
        //                SVGPathPoint p = new SVGPathPoint();
        //                p.CommandType = "rect";
        //                Dictionary<string, float> boxDets = new Dictionary<string, float>();
        //                boxDets["X"] = (X + (width * 0.5f));
        //                boxDets["Y"] = (Y - (height * 0.5f));
        //                boxDets["width"] = width;
        //                boxDets["height"] = height;
        //                p.ShapeDetails = boxDets;
        //                svgTokens.Add(p);
        //                //Debug.WriteLine($"svgTokens add rect: {X}, {Y}, {width}, {height} : svgTokens count: {svgTokens.Count}");
        //            }
        //            else if (key.Name.LocalName == "ellipse")
        //            {
        //                var X = float.Parse(key.Attribute("cx").Value) * Application.PixelSize;
        //                var Y = docSizeH - float.Parse(key.Attribute("cy").Value) * Application.PixelSize;
        //                //  should do something smart if width & height are different
        //                var width = float.Parse(key.Attribute("rx").Value) * Application.PixelSize;
        //                var height = float.Parse(key.Attribute("ry").Value) * Application.PixelSize;
        //                SVGPathPoint p = new SVGPathPoint();
        //                p.CommandType = "ellipse";
        //                Dictionary<string, float> ellipseDets = new Dictionary<string, float>();
        //                ellipseDets["X"] = X; //(X + (width * 0.5f));
        //                ellipseDets["Y"] = Y; //(Y - (height * 0.5f));
        //                ellipseDets["width"] = width;
        //                ellipseDets["height"] = height;
        //                p.ShapeDetails = ellipseDets;
        //                svgTokens.Add(p);
        //                //Debug.WriteLine($"svgTokens add ellipse: {X}, {Y}, {width}, {height} : svgTokens count: {svgTokens.Count}");
        //            }
        //            else if (key.Name.LocalName == "circle")
        //            {
        //                var X = float.Parse(key.Attribute("cx").Value) * Application.PixelSize;
        //                var Y = docSizeH - float.Parse(key.Attribute("cy").Value) * Application.PixelSize;
        //                var rad = float.Parse(key.Attribute("r").Value) * Application.PixelSize;
        //                SVGPathPoint p = new SVGPathPoint();
        //                p.CommandType = "circle";
        //                Dictionary<string, float> ellipseDets = new Dictionary<string, float>();
        //                ellipseDets["X"] = X; // (X + (rad * 0.5f));
        //                ellipseDets["Y"] = Y; // (Y + (rad * 0.5f));
        //                ellipseDets["radius"] = rad;
        //                p.ShapeDetails = ellipseDets;
        //                svgTokens.Add(p);
        //                //Debug.WriteLine($"svgTokens add ellipse: {X}, {Y}, {rad} : svgTokens count: {svgTokens.Count}");
        //            }
        //            else if (key.Name.LocalName == "g")
        //            {
        //                SVGDataDict plist = new SVGDataDict();
        //                var gElements = key.Elements();
        //                ParseSVG(plist, key.Elements(), w, docSizeH);
        //                List<SVGPathPoint> svgTokens2 = plist["svgTokens"];
        //                svgTokens = svgTokens.Concat(svgTokens2).ToList();
        //            }
        //            else
        //            {
        //                Debug.WriteLine($"TODO: Unimplemented Key Name: {key.Name}");
        //            }
        //        }
        //        catch (Exception e)
        //        {
        //            var ee = e.Message;
        //        }
        //    }
        //    dict["svgTokens"] = svgTokens;
        //}

        static Regex digitsOnly = new Regex(@"[^\d]");
        string RgbToHex(string rgbVal)
        {
            var splitString = rgbVal.Split(',');
            if (splitString.Length < 3)
                return "000000";
            var splitInts = splitString.Select(item => int.Parse(digitsOnly.Replace(item, ""))).ToArray();
            return splitInts[0].ToString("X2") + splitInts[1].ToString("X2") + splitInts[2].ToString("X2");
        }

        //public List<Vector2> Calculate_bezier(Vector2[] p, uint steps = 30)
        public List<Vector2> Calculate_bezier(List<Vector2> p, uint steps = 30)
        {
            if (steps == 0)
            {
                return null;
            }
            Vector2 f = new Vector2();
            Vector2 fd = new Vector2();
            Vector2 fdd = new Vector2();
            Vector2 fddd = new Vector2();
            Vector2 fdd_per_2 = new Vector2();
            Vector2 fddd_per_2 = new Vector2();
            Vector2 fddd_per_6 = new Vector2();
            float t = 1.0f / (float)steps;
            float t2 = t * t;
            f = p[0];
            fd = 3.0f * t * (p[1] - p[0]);
            fdd_per_2 = 3.0f * t2 * (p[0] - 2.0f * p[1] + p[2]);
            fddd_per_2 = 3.0f * t2 * t * (3.0f * (p[1] - p[2]) + p[3] - p[0]);

            fddd = fddd_per_2 + fddd_per_2;
            fdd = fdd_per_2 + fdd_per_2;
            fddd_per_6 = (1.0f / 3) * fddd_per_2;

            List<Vector2> drawingPoints = new List<Vector2>();
            for (uint loop = 0; loop < steps; loop++)
            {
                f = f + fd + fdd_per_2 + fddd_per_6;
                fd = fd + fdd + fddd_per_2;
                fdd = fdd + fddd;
                fdd_per_2 = fdd_per_2 + fddd_per_2;
                drawingPoints.Add(f);
            }
            return drawingPoints;
        }
    }
}
