using UnityEngine;
using System;
using System.Collections;

// Defines a heap item that can be compared with other items 
public interface IHeapItem<T> : IComparable<T>
{   // The index of this item in the heap array.
    int HeapIndex
    {
        get;
        set;
    }
}

// Binary heap used as a priority queue, where the item with the highest priority is at index 0.
public class Heap<T> where T : IHeapItem<T>
{
    T[] items; // array that stores heap items. 
    int currentItemCount; // number of items currently in the heap.

    // Initializes a new heap
    public Heap(int maxHeapSize) => items = new T[maxHeapSize];

    public void Add(T item)
    {   // Adds item to heap, then sorts up to restore order
        item.HeapIndex = currentItemCount;
        items[currentItemCount] = item;
        SortUp(item);
        currentItemCount++;
    }

    public T RemoveFirst()
    {   // Removes and returns the root item, sorts down to restore order
        T firstItem = items[0];
        currentItemCount--;
        items[0] = items[currentItemCount];
        items[0].HeapIndex = 0;
        SortDown(items[0]);
        return firstItem;
    }

    // Repositions an item upward after its priority increases. 
    public void UpdateItem(T item) => SortUp(item);

    // Returns how many items are currently in the heap.
    public int Count => currentItemCount;

    // Returns whether the heap has this item
    public bool Contains(T item) => Equals(items[item.HeapIndex], item);

    // Moves item downward while it has lower priority than either of its children.
    void SortDown(T item)
    {
        while (true)
        {
            int childIndexLeft = item.HeapIndex * 2 + 1;
            int childIndexRight = item.HeapIndex * 2 + 2;
            int swapIndex = 0;
            if (childIndexLeft < currentItemCount)
            {   // If the left child exists, check if it has higher priority than the item.
                swapIndex = childIndexLeft;
                if (childIndexRight < currentItemCount && items[childIndexLeft].CompareTo(items[childIndexRight]) < 0)
                {   // If the right child exists and has higher priority than the left child, use the right child instead.
                    swapIndex = childIndexRight;
                }   // If the item has lower priority than the highest-priority child, swap them. 
                if (item.CompareTo(items[swapIndex]) < 0) Swap(item, items[swapIndex]);
                else return;
            }
            else return;
        }
    }

    void SortUp(T item)
    {   // Moves item upward while it has a higher priority than its parent
        int parentIndex = (item.HeapIndex - 1) / 2;
        while (true)
        {
            T parentItem = items[parentIndex];
            if (item.CompareTo(parentItem) > 0) Swap(item, parentItem);
            else break;
            parentIndex = (item.HeapIndex - 1) / 2;
        }
    }

    void Swap(T itemA, T itemB)
    {   // Swaps two items in the array, then updates their heap indexes
        items[itemA.HeapIndex] = itemB;
        items[itemB.HeapIndex] = itemA;
        int itemAIndex = itemA.HeapIndex;
        itemA.HeapIndex = itemB.HeapIndex;
        itemB.HeapIndex = itemAIndex;
    }
}
