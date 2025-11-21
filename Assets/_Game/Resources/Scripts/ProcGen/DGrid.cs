using UnityEngine;
using System.Collections.Generic;


public struct VectDir{

	public VectDir(Vector3 v, int i){
		vect = v;
		index = i;
	}
	public Vector3 vect;
	public int index;
}

public class GridCoord {
	public int x;
	public int y;
	
	public GridCoord(int xc, int yc) {
		x = xc;
		y = yc;
	}

	public static GridCoord Zero{
		get {return new GridCoord(0,0);}
	}

	public bool Equals(GridCoord other){
		return (this.x == other.x) && (this.y == other.y);
	}
	
	public static GridCoord operator +(GridCoord xc, GridCoord yc) {
		return new GridCoord(xc.x + yc.x, xc.y + yc.y);
	}

	public static GridCoord operator -(GridCoord xc, GridCoord yc) {
		return new GridCoord(xc.x - yc.x, xc.y - yc.y);
	}
	
	public static GridCoord operator *(int i, GridCoord yc) {
		return new GridCoord( yc.x * i, yc.y * i);
	}
}

public class Direction {

	private static Direction _instance;
	public GridCoord[] dirs = null;

	public static Direction Instance {
		get{
			if(_instance == null)
				_instance = new Direction();

			return _instance;
		}
		private set{}
	}

	protected Direction(){
		dirs = new GridCoord[4];

		dirs[0] = new GridCoord(0,  1);  // North
		dirs[1] = new GridCoord(1,  0);  // East
		dirs[2] = new GridCoord(0, -1); // South
		dirs[3] = new GridCoord(-1, 0); // West
	}

	public static int Polar(int i){

		//return i * -1;


		if(i < 2)
			return i + 2;
		else
			return i - 2;

	}
}


public class DGrid {

	private int mapSizeX, mapSizeY;
	private int[,] dGrid;
	private GridCoord[] dirs;
	
	public DGrid(int x, int y){
		mapSizeX = x;
		mapSizeY = y;

		InitGrid();
	}

	public void InitGrid(){

		dGrid = new int[mapSizeX, mapSizeY];
		dirs = Direction.Instance.dirs;

		for(int x = 0; x < mapSizeX; x++){
			for(int y = mapSizeY; y < mapSizeY; y++){
				dGrid[x,y] = 0;
			}
		}
	}

	public void SetMapSize(int x, int y){
		mapSizeX = x;
		mapSizeY = y;
	}

	public bool PlaceAt(Vector3 v){

		if(isValidInput(v)){
			GridCoord pos = VecToGridCoord(v);
			dGrid[pos.x, pos.y] = 1; //Cell now represens a room in the grid. 
			//Debug.Log ("Placing cell at: "+pos.x+", "+pos.y);
			return true;
		}

		return false;
	}




    public VectDir NextRoomPos(Vector3 v)
    {
        if (isValidInput(v))
        {
            List<int> validDirections = new List<int>();
            GridCoord nextGC;

            // Collect all valid directions
            for (int i = 0; i < dirs.Length; i++)
            {
                nextGC = VecToGridCoord(v) + dirs[i];

                // look for empty spots
                if (isValidInput(nextGC) && dGrid[nextGC.x, nextGC.y] == 0)
                {
                    validDirections.Add(i);
                }
            }

            // If we have at least one possible direction, choose one at random
            if (validDirections.Count > 0)
            {
                int randomIndex = Random.Range(0, validDirections.Count);
                int d = validDirections[randomIndex];      // this is the direction (0..3)
                Vector3 nextV = v + GridCoordToVec(dirs[d]);
                return new VectDir(nextV, d);
            }
        }

        // No valid direction found
        return new VectDir(new Vector3(-1, -1, -1), 0);
    }

    public List<GridCoord> getNeighbors(Vector3 v){

		if(isValidInput(v))
		{
			List<GridCoord> neighbors = new List<GridCoord>();
			GridCoord nextGC;

			for(int i = 0; i < dirs.Length; i++)
			{
				nextGC = VecToGridCoord(v) + dirs[i];
				
				if(isValidInput(nextGC) && dGrid[nextGC.x, nextGC.y] == 1){
					neighbors.Add( new GridCoord(nextGC.x, nextGC.y));
				}
			}

			return neighbors;
		}

		return null;
	}


	public void MarkInvalidNeighbors(Vector3 v) 
	{
		if(isValidInput(v))
		{

			GridCoord roomsA; 
			GridCoord roomsB;

			for(int i = 0; i < dirs.Length; i++)
			{
				GridCoord next = VecToGridCoord(v) + dirs[i];

				roomsA = next + Reverse(dirs[i]);
				roomsB = next + Reverse (-1*dirs[i]);

				if(isValidInput(roomsA) && isValidInput(roomsB)){
					if(dGrid[roomsA.x,roomsA.y] == 1 || dGrid[roomsB.x, roomsB.y] == 1){
						dGrid[next.x, next.y] = 1; 
					}
				}
			}
			
		}
	}

	public bool CellsAvailable(Vector3 v){

		if(isValidInput(v))
		{
			MarkInvalidNeighbors(v);

			for(int i = 0; i < dirs.Length; i++)
			{
				GridCoord nextGC = VecToGridCoord(v) + dirs[i];
				
				if(isValidInput(nextGC) && dGrid[nextGC.x, nextGC.y] == 0){
					return true;
				}
			}
		}	
		return false;
	}

	public bool isValidInput(Vector3 v){
		
		if((0 > v.x || v.x >= mapSizeX) || (0 > v.z || v.z >= mapSizeY))
			return false;
		
		return true;
	}
	
	public bool isValidInput(GridCoord gc){
		
		if((0 > gc.x || gc.x >= mapSizeX) || (0 > gc.y || gc.y >= mapSizeY))
			return false;
		
		return true;
	}

	public static GridCoord VecToGridCoord(Vector3 v){
		
		return new GridCoord((int)Mathf.Ceil(v.x), (int)Mathf.Ceil(v.z));
	}
	
	public static Vector3 GridCoordToVec(GridCoord gc){
		return new Vector3(gc.x, 0, gc.y);
	}
	
	public GridCoord Reverse(GridCoord gc){
		return new GridCoord(gc.y, gc.x);
	}


}
