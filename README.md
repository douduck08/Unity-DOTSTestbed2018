### The testbed of Unity DOTS
Data-Oriented Technology Stack (DOTS) are Unity 2018 new features about multithread programming, include:
* C# Job System
* Entity Component System (ECS)
* Burst compiler

This project used Unity version: 2018.1.9f2

### Samples
#### 1.Gravity
![Gravity](https://github.com/douduck08/Unity-DOTSTestbed/blob/master/image/gravity.png)
Apply force and move 125000 cubes.
* MonoBehaviour Update: 72.1 ms
* DOTS Update: 6.5 ms

#### 2.Chain with wave
![Gravity](https://github.com/douduck08/Unity-DOTSTestbed/blob/master/image/chain.png)
Update local position of 8100 chain of cubes, 10 nested objects in 1 chain, totally 81000 cubes.
* MonoBehaviour Update: 22.3 ms
* DOTS Update: 3.6 ms

#### 3.Dynamic bone
![Gravity](https://github.com/douduck08/Unity-DOTSTestbed/blob/master/image/dynamic_bone.png)
Update 2500 simplified dynamic bones, 6 particles in 1 bone, totally 15000 particles.
* MonoBehaviour Update: 15.0 ms
* DOTS Update: 6.3 ms

#### 4.Distance check
![Gravity](https://github.com/douduck08/Unity-DOTSTestbed/blob/master/image/gravity.png)
Test disabling update when objects out of distance.

### Ref
在 Entity 中儲存 Array
* https://forum.unity.com/threads/store-an-array-in-icomponentdata.587137/
* https://forum.unity.com/threads/how-to-initialize-components-in-fixedarray.538218/

他者 Entity 之 ComponentData 存取
  * 【Unity,ECS】他のEntityが持つComponentDataを追跡する - http://tsubakit1.hateblo.jp/entry/2018/12/02/000705