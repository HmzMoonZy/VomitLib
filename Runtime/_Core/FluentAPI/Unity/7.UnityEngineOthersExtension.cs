/****************************************************************************
 * Copyright (c) 2016 - 2023 liangxiegame UNDER MIT License
 * 
 * http://qframework.cn
 * https://github.com/liangxiegame/QFramework
 * https://gitee.com/liangxiegame/QFramework
 ****************************************************************************/

using System.Collections.Generic;
using UnityEngine;

namespace FluentAPI
{

    public static class UnityEngineOthersExtension
    {

        public static T GetRandomItem<T>(this List<T> list)
        {
            return list[UnityEngine.Random.Range(0, list.Count)];
        }
        

        public static T GetAndRemoveRandomItem<T>(this List<T> list)
        {
            var randomIndex = UnityEngine.Random.Range(0, list.Count);
            var randomItem = list[randomIndex];
            list.RemoveAt(randomIndex);
            return randomItem;
        }


        public static SpriteRenderer Alpha(this SpriteRenderer self, float alpha)
        {
            var color = self.color;
            color.a = alpha;
            self.color = color;
            return self;
        }


        public static float Lerp(this float self, float a, float b)
        {
            return Mathf.Lerp(a, b, self);
        }


        public static float Abs(this float self)
        {
            return Mathf.Abs(self);
        }
        
        public static float Abs(this int self)
        {
            return Mathf.Abs(self);
        }


        public static float Sign(this float self)
        {
            return Mathf.Sign(self);
        }
        
        public static float Sign(this int self)
        {
            return Mathf.Sign(self);
        }
        

        public static float Cos(this float self)
        {
            return Mathf.Cos(self);
        }
        
        public static float Cos(this int self)
        {
            return Mathf.Cos(self);
        }
        

        public static float Sin(this float self)
        {
            return Mathf.Sin(self);
        }
        
        public static float Sin(this int self)
        {
            return Mathf.Sin(self);
        }


        public static float CosAngle(this float self)
        {
            return Mathf.Cos(self * Mathf.Deg2Rad);
        }
        
        public static float CosAngle(this int self)
        {
            return Mathf.Cos(self * Mathf.Deg2Rad);
        }


        public static float SinAngle(this float self)
        {
            return Mathf.Sin(self * Mathf.Deg2Rad);
        }
        
        public static float SinAngle(this int self)
        {
            return Mathf.Sin(self * Mathf.Deg2Rad);
        }


        public static float Deg2Rad(this float self)
        {
            return self * Mathf.Deg2Rad;
        }

        public static float Deg2Rad(this int self)
        {
            return self * Mathf.Deg2Rad;
        }
        
        

        public static float Rad2Deg(this float self)
        {
            return self * Mathf.Rad2Deg;
        }
        
        public static float Rad2Deg(this int self)
        {
            return self * Mathf.Rad2Deg;
        }
        

                
        public static Vector2 AngleToDirection2D(this int self)
        {
            return new Vector2(self.CosAngle(), self.SinAngle());
        }
        
        public static Vector2 AngleToDirection2D(this float self)
        {
            return new Vector2(self.CosAngle(), self.SinAngle());
        }
        

        public static float ToAngle(this Vector2 self)
        {
            return Mathf.Atan2(self.y, self.x).Rad2Deg();
        }
    }


    public static class RandomUtility
    {

        public static T Choose<T>(params T[] args)
        {
            return args[UnityEngine.Random.Range(0, args.Length)];
        }
    }
}