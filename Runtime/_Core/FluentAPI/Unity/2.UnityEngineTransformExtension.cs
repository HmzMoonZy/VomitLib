/****************************************************************************
 * Copyright (c) 2015 - 2022 liangxiegame UNDER MIT License
 *
 * http://qframework.cn
 * https://github.com/liangxiegame/QFramework
 * https://gitee.com/liangxiegame/QFramework
 ****************************************************************************/

using System;
using UnityEngine;

namespace FluentAPI
{

    public static class UnityEngineTransformExtension
    {

        public static T Parent<T>(this T self, Component parent) where T : Component
        {
            self.transform.SetParent(parent == null ? null : parent.transform);
            return self;
        }


        public static GameObject Parent(this GameObject self, Component parent)
        {
            self.transform.SetParent(parent == null ? null : parent.transform);
            return self;
        }


        public static T AsRootTransform<T>(this T self) where T : Component
        {
            self.transform.SetParent(null);
            return self;
        }


        public static GameObject AsRootGameObject<T>(this GameObject self)
        {
            self.transform.SetParent(null);
            return self;
        }



        public static T LocalIdentity<T>(this T self) where T : Component
        {
            self.transform.localPosition = Vector3.zero;
            self.transform.localRotation = Quaternion.identity;
            self.transform.localScale = Vector3.one;
            return self;
        }


        public static GameObject LocalIdentity(this GameObject self)
        {
            self.transform.localPosition = Vector3.zero;
            self.transform.localRotation = Quaternion.identity;
            self.transform.localScale = Vector3.one;
            return self;
        }



        public static T LocalPosition<T>(this T selfComponent, Vector3 localPos) where T : Component
        {
            selfComponent.transform.localPosition = localPos;
            return selfComponent;
        }

        public static GameObject LocalPosition(this GameObject self, Vector3 localPos)
        {
            self.transform.localPosition = localPos;
            return self;
        }


        public static Vector3 LocalPosition<T>(this T selfComponent) where T : Component
        {
            return selfComponent.transform.localPosition;
        }


        public static Vector3 LocalPosition(this GameObject self)
        {
            return self.transform.localPosition;
        }



        public static T LocalPosition<T>(this T selfComponent, float x, float y, float z) where T : Component
        {
            selfComponent.transform.localPosition = new Vector3(x, y, z);
            return selfComponent;
        }


        public static GameObject LocalPosition(this GameObject self, float x, float y, float z)
        {
            self.transform.localPosition = new Vector3(x, y, z);
            return self;
        }



        public static T LocalPosition<T>(this T selfComponent, float x, float y) where T : Component
        {
            var localPosition = selfComponent.transform.localPosition;
            localPosition.x = x;
            localPosition.y = y;
            selfComponent.transform.localPosition = localPosition;
            return selfComponent;
        }


        public static GameObject LocalPosition(this GameObject self, float x, float y)
        {
            var localPosition = self.transform.localPosition;
            localPosition.x = x;
            localPosition.y = y;
            self.transform.localPosition = localPosition;
            return self;
        }


        public static T LocalPositionX<T>(this T selfComponent, float x) where T : Component
        {
            var localPosition = selfComponent.transform.localPosition;
            localPosition.x = x;
            selfComponent.transform.localPosition = localPosition;
            return selfComponent;
        }


        public static GameObject LocalPositionX(this GameObject self, float x)
        {
            var localPosition = self.transform.localPosition;
            localPosition.x = x;
            self.transform.localPosition = localPosition;
            return self;
        }


        public static T LocalPositionY<T>(this T selfComponent, float y) where T : Component
        {
            var localPosition = selfComponent.transform.localPosition;
            localPosition.y = y;
            selfComponent.transform.localPosition = localPosition;
            return selfComponent;
        }



        public static GameObject LocalPositionY(this GameObject selfComponent, float y)
        {
            var localPosition = selfComponent.transform.localPosition;
            localPosition.y = y;
            selfComponent.transform.localPosition = localPosition;
            return selfComponent;
        }



        public static T LocalPositionZ<T>(this T selfComponent, float z) where T : Component
        {
            var localPosition = selfComponent.transform.localPosition;
            localPosition.z = z;
            selfComponent.transform.localPosition = localPosition;
            return selfComponent;
        }


        public static GameObject LocalPositionZ(this GameObject self, float z)
        {
            var localPosition = self.transform.localPosition;
            localPosition.z = z;
            self.transform.localPosition = localPosition;
            return self;
        }



        public static T LocalPositionIdentity<T>(this T selfComponent) where T : Component
        {
            selfComponent.transform.localPosition = Vector3.zero;
            return selfComponent;
        }


        public static GameObject LocalPositionIdentity(this GameObject self)
        {
            self.transform.localPosition = Vector3.zero;
            return self;
        }



        public static Quaternion LocalRotation<T>(this T selfComponent) where T : Component
        {
            return selfComponent.transform.localRotation;
        }


        public static Quaternion LocalRotation(this GameObject self)
        {
            return self.transform.localRotation;
        }


        public static T LocalRotation<T>(this T selfComponent, Quaternion localRotation) where T : Component
        {
            selfComponent.transform.localRotation = localRotation;
            return selfComponent;
        }


        public static GameObject LocalRotation(this GameObject selfComponent, Quaternion localRotation)
        {
            selfComponent.transform.localRotation = localRotation;
            return selfComponent;
        }


        public static T LocalRotationIdentity<T>(this T selfComponent) where T : Component
        {
            selfComponent.transform.localRotation = Quaternion.identity;
            return selfComponent;
        }


        public static GameObject LocalRotationIdentity(this GameObject selfComponent)
        {
            selfComponent.transform.localRotation = Quaternion.identity;
            return selfComponent;
        }


        public static T LocalScale<T>(this T selfComponent, Vector3 scale) where T : Component
        {
            selfComponent.transform.localScale = scale;
            return selfComponent;
        }


        public static GameObject LocalScale(this GameObject self, Vector3 scale)
        {
            self.transform.localScale = scale;
            return self;
        }



        public static Vector3 LocalScale<T>(this T selfComponent) where T : Component
        {
            return selfComponent.transform.localScale;
        }


        public static Vector3 LocalScale(this GameObject self)
        {
            return self.transform.localScale;
        }



        public static T LocalScale<T>(this T selfComponent, float xyz) where T : Component
        {
            selfComponent.transform.localScale = Vector3.one * xyz;
            return selfComponent;
        }


        public static GameObject LocalScale(this GameObject self, float xyz)
        {
            self.transform.localScale = Vector3.one * xyz;
            return self;
        }


        public static T LocalScale<T>(this T selfComponent, float x, float y, float z) where T : Component
        {
            var localScale = selfComponent.transform.localScale;
            localScale.x = x;
            localScale.y = y;
            localScale.z = z;
            selfComponent.transform.localScale = localScale;
            return selfComponent;
        }


        public static GameObject LocalScale(this GameObject selfComponent, float x, float y, float z)
        {
            var localScale = selfComponent.transform.localScale;
            localScale.x = x;
            localScale.y = y;
            localScale.z = z;
            selfComponent.transform.localScale = localScale;
            return selfComponent;
        }


        public static T LocalScale<T>(this T selfComponent, float x, float y) where T : Component
        {
            var localScale = selfComponent.transform.localScale;
            localScale.x = x;
            localScale.y = y;
            selfComponent.transform.localScale = localScale;
            return selfComponent;
        }


        public static GameObject LocalScale(this GameObject selfComponent, float x, float y)
        {
            var localScale = selfComponent.transform.localScale;
            localScale.x = x;
            localScale.y = y;
            selfComponent.transform.localScale = localScale;
            return selfComponent;
        }


        public static T LocalScaleX<T>(this T selfComponent, float x) where T : Component
        {
            var localScale = selfComponent.transform.localScale;
            localScale.x = x;
            selfComponent.transform.localScale = localScale;
            return selfComponent;
        }



        public static GameObject LocalScaleX(this GameObject self, float x)
        {
            var localScale = self.transform.localScale;
            localScale.x = x;
            self.transform.localScale = localScale;
            return self;
        }


        public static float LocalScaleX(this GameObject self)
        {
            return self.transform.localScale.x;
        }


        public static float LocalScaleX<T>(this T self) where T : Component
        {
            return self.transform.localScale.x;
        }



        public static T LocalScaleY<T>(this T self, float y) where T : Component
        {
            var localScale = self.transform.localScale;
            localScale.y = y;
            self.transform.localScale = localScale;
            return self;
        }


        public static GameObject LocalScaleY(this GameObject self, float y)
        {
            var localScale = self.transform.localScale;
            localScale.y = y;
            self.transform.localScale = localScale;
            return self;
        }


        public static float LocalScaleY<T>(this T self) where T : Component
        {
            return self.transform.localScale.y;
        }


        public static float LocalScaleY(this GameObject self)
        {
            return self.transform.localScale.y;
        }



        public static T LocalScaleZ<T>(this T selfComponent, float z) where T : Component
        {
            var localScale = selfComponent.transform.localScale;
            localScale.z = z;
            selfComponent.transform.localScale = localScale;
            return selfComponent;
        }


        public static GameObject LocalScaleZ(this GameObject selfComponent, float z)
        {
            var localScale = selfComponent.transform.localScale;
            localScale.z = z;
            selfComponent.transform.localScale = localScale;
            return selfComponent;
        }


        public static float LocalScaleZ<T>(this T self) where T : Component
        {
            return self.transform.localScale.z;
        }


        public static float LocalScaleZ(this GameObject self)
        {
            return self.transform.localScale.z;
        }



        public static T LocalScaleIdentity<T>(this T selfComponent) where T : Component
        {
            selfComponent.transform.localScale = Vector3.one;
            return selfComponent;
        }

        public static GameObject LocalScaleIdentity(this GameObject selfComponent)
        {
            selfComponent.transform.localScale = Vector3.one;
            return selfComponent;
        }


        public static T Identity<T>(this T selfComponent) where T : Component
        {
            selfComponent.transform.position = Vector3.zero;
            selfComponent.transform.rotation = Quaternion.identity;
            selfComponent.transform.localScale = Vector3.one;
            return selfComponent;
        }


        public static GameObject Identity(this GameObject self)
        {
            self.transform.position = Vector3.zero;
            self.transform.rotation = Quaternion.identity;
            self.transform.localScale = Vector3.one;
            return self;
        }


        public static T Position<T>(this T selfComponent, Vector3 position) where T : Component
        {
            selfComponent.transform.position = position;
            return selfComponent;
        }


        public static GameObject Position(this GameObject self, Vector3 position)
        {
            self.transform.position = position;
            return self;
        }



        public static Vector3 Position<T>(this T selfComponent) where T : Component
        {
            return selfComponent.transform.position;
        }


        public static Vector3 Position(this GameObject self)
        {
            return self.transform.position;
        }


        public static T Position<T>(this T selfComponent, float x, float y, float z) where T : Component
        {
            selfComponent.transform.position = new Vector3(x, y, z);
            return selfComponent;
        }


        public static GameObject Position(this GameObject self, float x, float y, float z)
        {
            self.transform.position = new Vector3(x, y, z);
            return self;
        }


        public static T Position<T>(this T selfComponent, float x, float y) where T : Component
        {
            var position = selfComponent.transform.position;
            position.x = x;
            position.y = y;
            selfComponent.transform.position = position;
            return selfComponent;
        }


        public static GameObject Position(this GameObject selfComponent, float x, float y)
        {
            var position = selfComponent.transform.position;
            position.x = x;
            position.y = y;
            selfComponent.transform.position = position;
            return selfComponent;
        }



        public static T PositionIdentity<T>(this T selfComponent) where T : Component
        {
            selfComponent.transform.position = Vector3.zero;
            return selfComponent;
        }


        public static GameObject PositionIdentity(this GameObject selfComponent)
        {
            selfComponent.transform.position = Vector3.zero;
            return selfComponent;
        }



        public static T PositionX<T>(this T selfComponent, float x) where T : Component
        {
            var position = selfComponent.transform.position;
            position.x = x;
            selfComponent.transform.position = position;
            return selfComponent;
        }


        public static GameObject PositionX(this GameObject self, float x)
        {
            var position = self.transform.position;
            position.x = x;
            self.transform.position = position;
            return self;
        }



        public static T PositionX<T>(this T selfComponent, Func<float, float> xSetter) where T : Component
        {
            var position = selfComponent.transform.position;
            position.x = xSetter(position.x);
            selfComponent.transform.position = position;
            return selfComponent;
        }


        public static GameObject PositionX(this GameObject self, Func<float, float> xSetter)
        {
            var position = self.transform.position;
            position.x = xSetter(position.x);
            self.transform.position = position;
            return self;
        }



        public static T PositionY<T>(this T selfComponent, float y) where T : Component
        {
            var position = selfComponent.transform.position;
            position.y = y;
            selfComponent.transform.position = position;
            return selfComponent;
        }

        public static GameObject PositionY(this GameObject self, float y)
        {
            var position = self.transform.position;
            position.y = y;
            self.transform.position = position;
            return self;
        }


        public static T PositionY<T>(this T selfComponent, Func<float, float> ySetter) where T : Component
        {
            var position = selfComponent.transform.position;
            position.y = ySetter(position.y);
            selfComponent.transform.position = position;
            return selfComponent;
        }


        public static GameObject PositionY(this GameObject self, Func<float, float> ySetter)
        {
            var position = self.transform.position;
            position.y = ySetter(position.y);
            self.transform.position = position;
            return self;
        }


        public static T PositionZ<T>(this T selfComponent, float z) where T : Component
        {
            var position = selfComponent.transform.position;
            position.z = z;
            selfComponent.transform.position = position;
            return selfComponent;
        }

        public static GameObject PositionZ(this GameObject self, float z)
        {
            var position = self.transform.position;
            position.z = z;
            self.transform.position = position;
            return self;
        }


        public static T PositionZ<T>(this T self, Func<float, float> zSetter) where T : Component
        {
            var position = self.transform.position;
            position.z = zSetter(position.z);
            self.transform.position = position;
            return self;
        }


        public static GameObject PositionZ(this GameObject self, Func<float, float> zSetter)
        {
            var position = self.transform.position;
            position.z = zSetter(position.z);
            self.transform.position = position;
            return self;
        }



        public static T RotationIdentity<T>(this T selfComponent) where T : Component
        {
            selfComponent.transform.rotation = Quaternion.identity;
            return selfComponent;
        }


        public static GameObject RotationIdentity(this GameObject selfComponent)
        {
            selfComponent.transform.rotation = Quaternion.identity;
            return selfComponent;
        }


        public static T Rotation<T>(this T selfComponent, Quaternion rotation) where T : Component
        {
            selfComponent.transform.rotation = rotation;
            return selfComponent;
        }


        public static GameObject Rotation(this GameObject self, Quaternion rotation)
        {
            self.transform.rotation = rotation;
            return self;
        }


        public static Quaternion Rotation<T>(this T selfComponent) where T : Component
        {
            return selfComponent.transform.rotation;
        }


        public static Quaternion Rotation(this GameObject self)
        {
            return self.transform.rotation;
        }



        public static Vector3 Scale<T>(this T selfComponent) where T : Component
        {
            return selfComponent.transform.lossyScale;
        }


        public static Vector3 Scale(this GameObject selfComponent)
        {
            return selfComponent.transform.lossyScale;
        }



        public static T DestroyChildren<T>(this T selfComponent) where T : Component
        {
            var childCount = selfComponent.transform.childCount;

            for (var i = 0; i < childCount; i++)
            {
                selfComponent.transform.GetChild(i).DestroyGameObjGracefully();
            }

            return selfComponent;
        }


        public static T DestroyChildrenWithCondition<T>(this T selfComponent, Func<Transform, bool> condition)
            where T : Component
        {
            var childCount = selfComponent.transform.childCount;

            for (var i = 0; i < childCount; i++)
            {
                var child = selfComponent.transform.GetChild(i);
                if (condition(child))
                {
                    child.DestroyGameObjGracefully();
                }
            }

            return selfComponent;
        }


        public static GameObject DestroyChildren(this GameObject selfGameObj)
        {
            var childCount = selfGameObj.transform.childCount;

            for (var i = 0; i < childCount; i++)
            {
                selfGameObj.transform.GetChild(i).DestroyGameObjGracefully();
            }

            return selfGameObj;
        }



        public static T AsLastSibling<T>(this T selfComponent) where T : Component
        {
            selfComponent.transform.SetAsLastSibling();
            return selfComponent;
        }


        public static GameObject AsLastSibling(this GameObject self)
        {
            self.transform.SetAsLastSibling();
            return self;
        }


        public static T AsFirstSibling<T>(this T selfComponent) where T : Component
        {
            selfComponent.transform.SetAsFirstSibling();
            return selfComponent;
        }

        public static GameObject AsFirstSibling(this GameObject selfComponent)
        {
            selfComponent.transform.SetAsFirstSibling();
            return selfComponent;
        }


        public static T SiblingIndex<T>(this T selfComponent, int index) where T : Component
        {
            selfComponent.transform.SetSiblingIndex(index);
            return selfComponent;
        }


        public static GameObject SiblingIndex(this GameObject selfComponent, int index)
        {
            selfComponent.transform.SetSiblingIndex(index);
            return selfComponent;
        }

        public static Vector2 Position2D(this GameObject self)
        {
            return new Vector2(self.transform.position.x, self.transform.position.y);
        }

        public static Vector2 Position2D(this Component self)
        {
            return new Vector2(self.transform.position.x, self.transform.position.y);
        }

        public static GameObject Position2D(this GameObject self, Vector2 position)
        {
            return self.Position(position.x, position.y);
        }

        public static T Position2D<T>(this T self, Vector2 position) where T : Component
        {
            return self.Position(position.x, position.y);
        }

        public static Vector2 LocalPosition2D(this GameObject self)
        {
            return new Vector2(self.transform.localPosition.x, self.transform.localPosition.y);
        }

        public static Vector2 LocalPosition2D(this Component self)
        {
            return new Vector2(self.transform.localPosition.x, self.transform.localPosition.y);
        }

        public static GameObject LocalPosition2D(this GameObject self, Vector2 position)
        {
            return self.LocalPosition(position.x, position.y);
        }

        public static T LocalPosition2D<T>(this T self, Vector2 position) where T : Component
        {
            return self.LocalPosition(position.x, position.y);
        }

        public static GameObject SyncPositionFrom(this GameObject self, GameObject from)
        {
            return self.Position(from.Position());
        }

        public static T SyncPositionFrom<T>(this T self, GameObject from) where T : Component
        {
            return self.Position(from.Position());
        }

        public static GameObject SyncPositionFrom<T>(this GameObject self, Component from) where T : Component
        {
            return self.Position(from.Position());
        }

        public static T SyncPositionFrom<T>(this T self, Component from) where T : Component
        {
            return self.Position(from.Position());
        }

        public static GameObject SyncPosition2DFrom(this GameObject self, GameObject from) =>
            self.Position2D(from.Position2D());

        public static T SyncPosition2DFrom<T>(this T self, GameObject from) where T : Component =>
            self.Position2D(from.Position2D());

        public static GameObject SyncPosition2DFrom(this GameObject self, Component from) =>
            self.Position2D(from.Position2D());

        public static T SyncPosition2DFrom<T>(this T self, Component from) where T : Component =>
            self.Position2D(from.Position2D());


        public static GameObject SyncPositionTo(this GameObject self, GameObject to)
        {
            to.Position(self.Position());
            return self;
        }

        public static GameObject SyncPositionTo(this GameObject self, Component to)
        {
            to.Position(self.Position());
            return self;
        }

        public static T SyncPositionTo<T>(this T self, GameObject to) where T : Component
        {
            to.Position(self.Position());
            return self;
        }

        public static T SyncPositionTo<T>(this T self, Component to) where T : Component
        {
            to.Position(self.Position());
            return self;
        }

        public static GameObject SyncPosition2DTo(this GameObject self, GameObject to)
        {
            to.Position2D(self.Position2D());
            return self;
        }

        public static GameObject SyncPosition2DTo(this GameObject self, Component to)
        {
            to.Position2D(self.Position2D());
            return self;
        }

        public static T SyncPosition2DTo<T>(this T self, GameObject to) where T : Component
        {
            to.Position2D(self.Position2D());
            return self;
        }

        public static T SyncPosition2DTo<T>(this T self, Component to) where T : Component
        {
            to.Position2D(self.Position2D());
            return self;
        }

        public static float PositionX(this GameObject self)
        {
            return self.transform.position.x;
        }

        public static float PositionX(this Component self)
        {
            return self.transform.position.x;
        }

        public static float PositionY(this GameObject self)
        {
            return self.transform.position.y;
        }

        public static float PositionY(this Component self)
        {
            return self.transform.position.y;
        }

        public static float PositionZ(this GameObject self)
        {
            return self.transform.position.z;
        }

        public static float PositionZ(this Component self)
        {
            return self.transform.position.z;
        }

        public static float LocalPositionX(this GameObject self)
        {
            return self.transform.localPosition.x;
        }

        public static float LocalPositionX(this Component self)
        {
            return self.transform.localPosition.x;
        }

        public static float LocalPositionY(this GameObject self)
        {
            return self.transform.localPosition.y;
        }

        public static float LocalPositionY(this Component self)
        {
            return self.transform.localPosition.y;
        }

        public static float LocalPositionZ(this GameObject self)
        {
            return self.transform.localPosition.z;
        }

        public static float LocalPositionZ(this Component self) => self.transform.localPosition.z;
        public static Vector3 LocalEulerAngles(this GameObject self) => self.transform.localEulerAngles;
        public static Vector3 LocalEulerAngles(this Component self) => self.transform.localEulerAngles;

        public static GameObject LocalEulerAngles(this GameObject self, Vector3 localEulerAngles)
        {
            self.transform.localEulerAngles = localEulerAngles;
            return self;
        }

        public static T LocalEulerAngles<T>(this T self, Vector3 localEulerAngles) where T : Component
        {
            self.transform.localEulerAngles = localEulerAngles;
            return self;
        }

        public static GameObject LocalEulerAnglesZ(this GameObject self, float z)
        {
            self.LocalEulerAngles(self.LocalEulerAngles().Z(z));
            return self;
        }

        public static T LocalEulerAnglesZ<T>(this T self, float z) where T : Component
        {
            self.LocalEulerAngles(self.LocalEulerAngles().Z(z));
            return self;
        }
        
        
        public static Vector3 EulerAngles(this GameObject self) => self.transform.eulerAngles;
        public static Vector3 EulerAngles(this Component self) => self.transform.eulerAngles;

        public static GameObject EulerAngles(this GameObject self, Vector3 eulerAngles)
        {
            self.transform.eulerAngles = eulerAngles;
            return self;
        }

        public static T EulerAngles<T>(this T self, Vector3 eulerAngles) where T : Component
        {
            self.transform.eulerAngles = eulerAngles;
            return self;
        }

        public static GameObject EulerAnglesZ(this GameObject self, float z)
        {
            self.EulerAngles(self.EulerAngles().Z(z));
            return self;
        }

        public static T EulerAnglesZ<T>(this T self, float z) where T : Component
        {
            self.EulerAngles(self.EulerAngles().Z(z));
            return self;
        }
    }
}