﻿// ContentConverter.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DwarfCorp
{
    /// <summary>
    ///     Serializes and deserializes content of type T from asset tags.
    /// </summary>
    /// <typeparam Name="T">The type of the object to convert</typeparam>
    public class ContentConverter<T> : JsonConverter
    {
        private readonly Dictionary<T, string> assetMap;
        private readonly ContentManager contentManager;

        public ContentConverter(ContentManager manager, Dictionary<T, string> assets)
        {
            contentManager = manager;
            assetMap = assets;
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (assetMap.ContainsKey((T) value))
            {
                writer.WriteValue(assetMap[(T) value]);
            }
            else
            {
                writer.WriteValue("");
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            var jObject = serializer.Deserialize<JValue>(reader);
            return (jObject.Value != null && (string) jObject.Value != "")
                ? contentManager.Load<T>(jObject.Value.ToString())
                : default(T);
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof (T);
        }
    }
}