using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

class MyTronBot
{
	private const string EAST = "East";
	private const string NORTH = "North";
	private const string SOUTH = "South";
	private const string WEST = "West";

	private static readonly Direction[] Directions = new[] {
		new Direction(1, 0),
		new Direction(0, 1),
		new Direction(-1, 0),
		new Direction(0, -1)
	};

	private static readonly string[] Moves = new[] { EAST, SOUTH, WEST, NORTH };
	private static int[,] ShadowWall;

	private static bool B(int x, int y)
	{
		return (ShadowWall[x, y] == 0) && !Map.IsWall(x, y);
	}

	private static bool CanMove(int x, int y)
	{
		if (B(x, y - 1))
		{
			return true;
		}
		if (B(x + 1, y))
		{
			return true;
		}
		if (B(x, y + 1))
		{
			return true;
		}
		if (B(x - 1, y))
		{
			return true;
		}

		return false;
	}

	private static double Max(double a, double b)
	{
		return a > b ? a : b;
	}

	private static double EvalNode(int hisx, int hisy)
	{
		var distanceToHim = MyMatrix[hisx, hisy];

		if (distanceToHim == Int32.MaxValue)
		{
			var howmanypointshecanreach = CalculateBlocksReachable(hisx, hisy);

			var howmanypointsicanreach = 0;

			for (int i = 0; i < Width; i++)
			{
				for (int j = 0; j < Height; j++)
				{
					if (MyMatrix[i, j] != Int32.MaxValue)
					{
						howmanypointsicanreach++;
					}

				}
			}

			return howmanypointsicanreach - howmanypointshecanreach;
		}

		CalculateDomain(hisx, hisy, HisMatrix);

		var mydomaincount = 0;
		var hisdomaincount = 0;
        
		for (int i = 0; i < Width; i++)
		{
			for (int j = 0; j < Height; j++)
			{
				if (MyMatrix[i, j] < HisMatrix[i, j])
				{
					mydomaincount++;
				}

				if (HisMatrix[i, j] < MyMatrix[i, j])
				{
					hisdomaincount++;
				}

				HisMatrix[i, j] = Int32.MaxValue;
			}
		}

		if (mydomaincount == hisdomaincount)
		{
			return -0.5*Rnd.NextDouble();
		}

		double zz = mydomaincount - hisdomaincount + 0.5/distanceToHim;

		return zz;



	}

	private static int Fall(int myx, int myy, int depth, int top, string positionkey)
	{
        if (depth >= top)
		{
			return CalculateBlocksReachable(myx, myy);
		}

		var max = -1;

		List<int> mymoves;

		if (Hash.ContainsKey(positionkey))
		{
			mymoves = new List<int>(from x in Hash[positionkey] select x.move);


			AddAnyNonPrecalculated(mymoves);
			Hash[positionkey] = MakeSortedList(false);

		}
		else
		{
			mymoves = Defaultmoves;
			Hash.Add(positionkey, MakeSortedList(false));
		}

		foreach (var i in mymoves)
		{
			var nextx = myx + Directions[i].X;
			var nexty = myy + Directions[i].Y;


			if (B(nextx, nexty))
			{
				ShadowWall[nextx, nexty] = 1;

				var val = Fall(nextx, nexty, depth + 1, top, positionkey + i);

				Hash[positionkey].Add(new Pair(i, val + .5 * i / 3));

				if (val > max)
				{
					max = val;
				}

				ShadowWall[nextx, nexty] = 0;

			}
		}

		return 1 + max;
	}

	private static bool C(int nx, int ny)
	{
		return (ShadowWall[nx, ny] == 0) && !Map.IsWall(nx, ny);
	}

	private static int CalculateBlocksReachable(int x, int y)
	{
		for (var i = 0; i < Directions.Length; i++)
		{
			var nx = x + Directions[i].X;
			var ny = y + Directions[i].Y;

			if (C(nx, ny))
			{
				FloodFillScanlineStack(nx, ny);
			}
		}

		int count = 0;

		for (int i = 0; i < Map.Width(); i++)
		{
			for (int j = 0; j < Map.Height(); j++)
			{
				if (ShadowWall[i, j] == 2)
				{
					count++;

					ShadowWall[i, j] = 0;

				}
			}
		}

		return count;
	}

	private static int Indexer;

	private static void Push(int x, int y)
	{
		Indexer++;
		XStack[Indexer] = x;
		YStack[Indexer] = y;
	}

	static void FloodFillScanlineStack(int x, int y)
	{
		Indexer = -1;

		Push(x, y);

		while (Indexer != -1)
		{
			x = XStack[Indexer];
			y = YStack[Indexer];

			Indexer--;

			var y1 = y;
			while (y1 >= 0 && C(x, y1))
				y1--;

			y1++;

			var spanLeft = false;
			var spanRight = false;

			while (y1 < Map.Height() && C(x, y1))
			{

				ShadowWall[x, y1] = 2;

				if (!spanLeft && x > 0 && C(x - 1, y1))
				{
					Push(x - 1, y1);
					spanLeft = true;
				}
				else if (spanLeft && x > 0 && !C(x - 1, y1))
				{
					spanLeft = false;
				}
				if (!spanRight && x < Map.Width() - 1 && C(x + 1, y1))
				{
					Push(x + 1, y1);
					spanRight = true;
				}
				else if (spanRight && x < Map.Width() - 1 && !C(x + 1, y1))
				{
					spanRight = false;
				}
				y1++;
			}
		}
	}

	private static int SurvivalMode(int myx, int myy, string positionkey)
	{
		var result = 0;

		var top = 8;
		long took = 0;

		const int max = 50;

		while (1000 - timer.ElapsedMilliseconds > 4 * took)
		{
			var mark = timer.ElapsedMilliseconds;

			result = MaximizeSpace(myx, myy, top++, positionkey);

			took = timer.ElapsedMilliseconds - mark;

			if (top >= max)
			{
				break;
			}

		}

		return result;
	}

	private static int MaximizeSpace(int myx, int myy, int top, string positionkey)
	{
		var max = -1;

		var result = 0;

		List<int> mymoves;


		if (Hash.ContainsKey(positionkey))
		{
			mymoves = new List<int>(from x in Hash[positionkey] select x.move);

			AddAnyNonPrecalculated(mymoves);
			Hash[positionkey] = MakeSortedList(false);

		}
		else
		{
			mymoves = Defaultmoves;
			Hash.Add(positionkey, MakeSortedList(false));
		}

		foreach (var i in mymoves)
		{
			var mynextx = myx + Directions[i].X;
			var mynexty = myy + Directions[i].Y;

			if (!Map.IsWall(mynextx, mynexty))
			{
				ShadowWall[mynextx, mynexty] = 1;

				var val = Fall(mynextx, mynexty, 0, top, positionkey + i);

				Hash[positionkey].Add(new Pair(i, val + .5 * i / 3));


				if (val > max)
				{
					max = val;
					result = i;
				}

				ShadowWall[mynextx, mynexty] = 0;
			}
		}

		return result;
	}

	static readonly Dictionary<string, RelativeSortedList<Pair>> Hash = new Dictionary<string, RelativeSortedList<Pair>>();


	private static double MiniMax(int myx, int myy, int hisx, int hisy, int depth, double alpha, double beta, int top, string positionkey)
	{
		if (myx == hisx && myy == hisy)
		{
			return 0.00;
		}

		bool maxCanMove = CanMove(myx, myy);
		bool minCanMove = CanMove(hisx, hisy);

		if (!(maxCanMove || minCanMove))
		{
			return 0.00;
		}

		if (!maxCanMove)
		{
			return -Int32.MaxValue;
		}

		if (!minCanMove)
		{
			return Int32.MaxValue;
		}
		
		if (depth == top)
		{
			return EvalNode(hisx, hisy);

		}

		List<int> mymoves;

		if (Hash.ContainsKey(positionkey))
		{
			mymoves = new List<int>(from x in Hash[positionkey] select x.move);

			AddAnyNonPrecalculated(mymoves);

			Hash[positionkey] = MakeSortedList(false);

		}
		else
		{
			mymoves = Defaultmoves;
			Hash.Add(positionkey, MakeSortedList(false));

		}
		
		foreach (var i in mymoves)
		{
			var mynextx = myx + Directions[i].X;
			var mynexty = myy + Directions[i].Y;

			if (B(mynextx, mynexty))
			{

				var alpha1 = -beta;
				var beta1 = -alpha;

				ShadowWall[mynextx, mynexty] = 1;

				if (depth == top - 1)
				{
					CalculateDomain(mynextx, mynexty, MyMatrix);

				}

				List<int> hesmoves;

				if (Hash.ContainsKey(positionkey + i))
				{
					hesmoves = new List<int>(from x in Hash[positionkey + i] select x.move);

					AddAnyNonPrecalculated(hesmoves);

					Hash[positionkey + i] = MakeSortedList(true);

				}
				else
				{
					hesmoves = Defaultmoves;
					Hash.Add(positionkey + i, MakeSortedList(true));

				}

				foreach (var j in hesmoves)
				{
					var hisnextx = hisx + Directions[j].X;
					var hisnexty = hisy + Directions[j].Y;

					if (B(hisnextx, hisnexty) || (hisnextx == mynextx && hisnexty == mynexty))
					{
						ShadowWall[hisnextx, hisnexty] = 1;

						var value = MiniMax(mynextx, mynexty, hisnextx, hisnexty, depth + 1, -beta1, -alpha1, top, positionkey + i + j);

						alpha1 = Max(alpha1, -value);

						Hash[positionkey + i].Add(new Pair(j, alpha1));

						if (hisnextx != mynextx || hisnexty != mynexty)
						{
							ShadowWall[hisnextx, hisnexty] = 0;
						}

						if (beta1 <= alpha1) 
						{
							break;
						}
					}
				}

				if (depth == top - 1)
				{
					for (int k = 0; k < Width; k++)
					{
						for (int m = 0; m < Height; m++)
						{
							MyMatrix[k, m] = Int32.MaxValue;

						}
					}
				}

				ShadowWall[mynextx, mynexty] = 0;

				alpha = Max(alpha, -alpha1);


				Hash[positionkey].Add(new Pair(i, alpha));

				if (beta <= alpha) 
				{
					break;
				}

			}
		}

        return alpha;
	}

	private static void AddAnyNonPrecalculated(List<int> mymoves)
	{
		if (!mymoves.Contains(0)) { mymoves.Add(0); }
		if (!mymoves.Contains(1)) { mymoves.Add(1); }
		if (!mymoves.Contains(2)) { mymoves.Add(2); }
		if (!mymoves.Contains(3)) { mymoves.Add(3); }
	}

	private static readonly List<int> Defaultmoves = new List<int>(new[] { 0, 1, 2, 3 });

	private static int Deepening(string positionkey, int maxtime)
	{
		var result = 0;

		const int maxdepht = 60;

		var top = 5;

		long lasttook = 0;

		while (maxtime - timer.ElapsedMilliseconds > 4 * lasttook)
		{
			var mark = timer.ElapsedMilliseconds;

			result = FindBestMove(positionkey, top++);

			var took = timer.ElapsedMilliseconds - mark;

			if (top >= maxdepht)
			{
				break;
			}
			lasttook = took;
			

		}

		return result;
	}

	private static int FindBestMove(string positionkey, int top)
	{
		double max = -UInt32.MaxValue;

		int myx = Map.MyX();
		int myy = Map.MyY();

		int hisx = Map.OpponentX();
		int hisy = Map.OpponentY();

		var result = 0;
		var guardResult = 0;

		bool movementsAvailable = false;
		bool foundAGoodOne = false;


		List<int> mymoves;

		if (Hash.ContainsKey(positionkey))
		{
			mymoves = new List<int>(from x in Hash[positionkey] select x.move);

			AddAnyNonPrecalculated(mymoves);
			Hash[positionkey] = MakeSortedList(false);

		}
		else
		{
			mymoves = Defaultmoves;
			Hash.Add(positionkey, MakeSortedList(false));
		}


		foreach (var i in mymoves)
		{
			var mynextx = myx + Directions[i].X;
			var mynexty = myy + Directions[i].Y;


			if (!Map.IsWall(mynextx, mynexty))
			{
				ShadowWall[mynextx, mynexty] = 1;

				movementsAvailable = true;
				guardResult = i;

				double min = UInt32.MaxValue;

				if (0 == top)
				{
					CalculateDomain(mynextx, mynexty, MyMatrix);
				}

				List<int> hesmoves;

				if (Hash.ContainsKey(positionkey + i))
				{
					hesmoves = new List<int>(from x in Hash[positionkey + i] select x.move);
					AddAnyNonPrecalculated(hesmoves);

					Hash[positionkey + i] = MakeSortedList(true);
				}
				else
				{
					hesmoves = Defaultmoves;
					Hash.Add(positionkey + i, MakeSortedList(true));
				}

				foreach (var j in new List<int>(hesmoves))
				{
					int hisnextx = hisx + Directions[j].X;
					int hisnexty = hisy + Directions[j].Y;


					if (!Map.IsWall(hisnextx, hisnexty) || (hisnextx == mynextx && hisnexty == mynexty))
					{
						ShadowWall[hisnextx, hisnexty] = 1;
						
						var value = MiniMax(mynextx, mynexty, hisnextx, hisnexty, 0, max, min, top, positionkey + i + j);
						
						if (hisnextx != mynextx || hisnexty != mynexty)
						{
							ShadowWall[hisnextx, hisnexty] = 0;
						}

						Hash[positionkey + i].Add(new Pair(j, value));

						if (value < min)
						{
							min = value;
						}
					}

				}

				if (0 == top)
				{
					for (int k = 0; k < Width; k++)
					{
						for (int m = 0; m < Height; m++)
						{
							MyMatrix[k, m] = Int32.MaxValue;

						}
					}
				}

				ShadowWall[mynextx, mynexty] = 0;


				Hash[positionkey].Add(new Pair(i, min));

				if (min > max)
				{
					max = min;
					result = i;
					foundAGoodOne = true;

				}

			}
		}

		if (movementsAvailable && !foundAGoodOne)
		{
			result = guardResult;
		}


		return result;
	}

	private static RelativeSortedList<Pair> MakeSortedList(bool asc)
	{
		return asc
				? new RelativeSortedList<Pair>((x, y) => x.value - y.value)
				: new RelativeSortedList<Pair>((x, y) => y.value - x.value);
	}


	private static void Init(int[,] matrix1, int[,] matrix2, int width, int height)
	{
		for (int i = 0; i < width; i++)
		{
			for (int j = 0; j < height; j++)
			{
				matrix1[i, j] = Int32.MaxValue;
				matrix2[i, j] = Int32.MaxValue;
			}
		}
	}

	private static void CalculateDomain(int x, int y, int[,] matrix)
	{
		var low = 0;
		var high = 0;

		XStack[0] = x;
		YStack[0] = y;

		matrix[x, y] = 0;

		while (low <= high)
		{
			var hx = XStack[low];
			var hy = YStack[low];

			for (var i = 0; i < Directions.Length; i++)
			{
				var nextx = hx + Directions[i].X;
				var nexty = hy + Directions[i].Y;

				if ((B(nextx, nexty)) && matrix[hx, hy] + 1 < matrix[nextx, nexty])
				{
					matrix[nextx, nexty] = matrix[hx, hy] + 1;

					high++;
					XStack[high] = nextx;
					YStack[high] = nexty;

				}
			}

			low++;

		}

	}

	private static int[] XStack, YStack;
    
	private static int[,] MyMatrix;
	private static int[,] HisMatrix;

	private static int Height;
	private static int Width;

	private static readonly int[,] M = new int[3, 3];
	private static readonly Random Rnd = new Random(DateTime.Now.Minute);
	private static Stopwatch timer;


	public static void Main()
	{
		InitVectors();

		var firstTime = true;

		timer = new Stopwatch();

		var survivalmode = false;

		int hislastx = 0;
		int hislasty = 0;
		string positionkey = "";

		int maxtime;

		while (true)
		{
			Map.Initialize();

			timer.Start();

			var hisx = Map.OpponentX();
			var hisy = Map.OpponentY();

			if (firstTime)
			{
				Height = Map.Height();
				Width = Map.Width();

				//WARNING: verify how big  upper bound can get
				XStack = new int[Int16.MaxValue];
				YStack = new int[Int16.MaxValue];

				ShadowWall = new int[Width, Height];

				MyMatrix = new int[Width, Height];
				HisMatrix = new int[Width, Height];

				Init(MyMatrix, HisMatrix, Width, Height);

				firstTime = false;

				hislastx = hisx - 1;
				hislasty = hisy;

				maxtime = 3000;
			}
			else
			{
				maxtime = 1000;
			}

			var myx = Map.MyX();
			var myy = Map.MyY();

			if (!survivalmode)
			{
				survivalmode = !IsAnyBodyOutThere(myx, myy, hisx, hisy);
			}

			int mymove;

			if (survivalmode)
			{
				mymove = SurvivalMode(myx, myy, positionkey);
			}
			else
			{
				mymove = Deepening(positionkey, maxtime);

                var hismove = M[hisx - hislastx + 1, hisy - hislasty + 1];
                positionkey = positionkey + mymove + hismove;
			}
            
			hislastx = hisx;
			hislasty = hisy;
            
			Map.MakeMove(Moves[mymove]);
            
			timer.Reset();

		}
	}

	private static void InitVectors()
	{
		M[1, 2] = 1; //south
		M[1, 0] = 3; //north
		M[2, 1] = 0; //east
		M[0, 1] = 2; //west
	}

	private static bool IsAnyBodyOutThere(int myx, int myy, int hisx, int hisy)
	{
        var low = 0;
		var high = 0;

		XStack[0] = myx;
		YStack[0] = myy;

		var matrix = new bool[Map.Width(), Map.Height()];

		while (low <= high)
		{
			var hx = XStack[low];
			var hy = YStack[low];

			for (var i = 0; i < Directions.Length; i++)
			{
				var nextx = hx + Directions[i].X;
				var nexty = hy + Directions[i].Y;

				if ((B(nextx, nexty) && !matrix[nextx, nexty]))
				{
					matrix[nextx, nexty] = true;

					high++;
					XStack[high] = nextx;
					YStack[high] = nexty;

				}

				if (nextx == hisx && nexty == hisy)
				{
					return true;
				}
			}

			low++;

		}

		return false;
	}

	#region Nested type: Direction

	private class Direction
	{
		public readonly int X;
		public readonly int Y;

		public Direction(int x, int y)
		{
			X = x;
			Y = y;
		}
	}

	public class RelativeSortedList<T> : List<T>
	{
		private readonly Func<T, T, double> compareTo;

		public RelativeSortedList(Func<T, T, double> compareTo)
		{
			this.compareTo = compareTo;
		}

		public new void Add(T Item)
		{
			if (Count == 0)
			{
				base.Add(Item);
				
				return;
			}
			if (compareTo(Item, this[Count - 1]) > 0)
			{
				base.Add(Item);
				return;
			}
			var min = 0;
			var max = Count - 1;
			while ((max - min) > 1)
			{
				
				var half = min + ((max - min) / 2);
				
				var comp = compareTo(Item, this[half]);
				if (comp == 0)
				{
					Insert(half, Item);
					return;
				}

				if (comp < 0) max = half;   
				else min = half;   
			}
			if (compareTo(Item, this[min]) <= 0) Insert(min, Item);
			else Insert(min + 1, Item);
		}


	}


	class Pair
	{
		public readonly int move;
		public readonly double value;

		public Pair(int move, double value)
		{
			this.move = move;
			this.value = value;
		}
	}
	#endregion
}