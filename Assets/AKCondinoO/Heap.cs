using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SebastianLague{public class Heap<T>where T:IHeapItem<T>{//  [https://www.youtube.com/watch?v=3Dw5d7PlcTM]
readonly Dictionary<int,T>items=new Dictionary<int,T>();int currentItemsCount;public int Count{get{return currentItemsCount;}}public bool Contains(T item){return items.TryGetValue(item.HeapIndex,out T current)&&Equals(current,item);}
public void Add(T item){
item.HeapIndex=currentItemsCount;items[currentItemsCount]=item;
SortUp(item);
currentItemsCount++;
}
void SortUp(T item){
_Loop:{
var parentIdx=(item.HeapIndex-1)/2;
var parentItm=items[parentIdx];
if(item.CompareTo(parentItm)>0){
Swap(item,parentItm);
}else{
goto _End;
}
goto _Loop;
}
_End:{}
}
public T RemoveFirst(){
T firstItem=items[0];
currentItemsCount--;
items[0]=items[currentItemsCount];items[0].HeapIndex=0;
SortDown(items[0]);
return firstItem;}
void SortDown(T item){
_Loop:{
var indexToSwap=0;
var chldIdxLft=item.HeapIndex*2+1;
var chldIdxRgt=item.HeapIndex*2+2;
if(chldIdxLft<currentItemsCount&&items.ContainsKey(chldIdxLft)){
indexToSwap=chldIdxLft;
if(chldIdxRgt<currentItemsCount&&items.ContainsKey(chldIdxRgt)){
if(items[chldIdxLft].CompareTo(items[chldIdxRgt])<0){
indexToSwap=chldIdxRgt;
}
}
if(item.CompareTo(items[indexToSwap])<0){
Swap(item,items[indexToSwap]);
}else{
goto _End;
}
}else{
goto _End;
}
goto _Loop;
}
_End:{}
}
void Swap(T itemA,T itemB){
items[itemA.HeapIndex]=itemB;
items[itemB.HeapIndex]=itemA;
int itemA_HeapIndex=itemA.HeapIndex;//  Não se esqueça de trocar o índice no próprio objeto/item! :c)
itemA.HeapIndex=itemB.HeapIndex;
itemB.HeapIndex=itemA_HeapIndex;
}
public void Clear(){
items.Clear();
currentItemsCount=0;
}
}
public interface IHeapItem<T>:IComparable<T>{
int HeapIndex{get;set;}
}
}