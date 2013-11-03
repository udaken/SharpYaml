﻿// Copyright (c) 2013 SharpYaml - Alexandre Mutel
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
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
// -------------------------------------------------------------------------------
// SharpYaml is a fork of YamlDotNet https://github.com/aaubry/YamlDotNet
// published with the following license:
// -------------------------------------------------------------------------------
// 
// Copyright (c) 2008, 2009, 2010, 2011, 2012 Antoine Aubry
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
// of the Software, and to permit persons to whom the Software is furnished to do
// so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
using System;
using System.Collections.Generic;
using SharpYaml.Events;
using SharpYaml.Schemas;
using SharpYaml.Serialization.Descriptors;
using SharpYaml.Serialization.Serializers;

namespace SharpYaml.Serialization
{

	/// <summary>
	/// A context used while deserializing.
	/// </summary>
	public class SerializerContext
	{
		private readonly SerializerSettings settings;
		private readonly ITagTypeRegistry tagTypeRegistry;
		private readonly ITypeDescriptorFactory typeDescriptorFactory;
		private readonly List<AnchorLateBinding> anchorLateBindings;
	    private readonly IMappingKeyTransform keyTransform;
	    private IEmitter emitter;
		internal readonly Stack<string> Anchors = new Stack<string>();
		internal readonly Stack<YamlStyle> styles = new Stack<YamlStyle>(); 
		internal int AnchorCount;

		/// <summary>
		/// Initializes a new instance of the <see cref="SerializerContext"/> class.
		/// </summary>
		/// <param name="serializer">The serializer.</param>
		internal SerializerContext(Serializer serializer)
		{
			Serializer = serializer;
			settings = serializer.Settings;
		    keyTransform = settings.KeyTransform;
			tagTypeRegistry = settings.tagTypeRegistry;
			ObjectFactory = settings.ObjectFactory;
			Schema = Settings.Schema;
			typeDescriptorFactory = new TypeDescriptorFactory(Settings.Attributes, Settings.EmitDefaultValues);
			anchorLateBindings = new List<AnchorLateBinding>();
		}

		/// <summary>
		/// Gets a value indicating whether we are in the context of serializing.
		/// </summary>
		/// <value><c>true</c> if we are in the context of serializing; otherwise, <c>false</c>.</value>
		public bool IsSerializing
		{
			get { return Writer != null; }
		}

		/// <summary>
		/// Gets the settings.
		/// </summary>
		/// <value>The settings.</value>
		public SerializerSettings Settings
		{
			get { return settings; }
		}

		/// <summary>
		/// Gets the schema.
		/// </summary>
		/// <value>The schema.</value>
		public IYamlSchema Schema { get; private set; }

		/// <summary>
		/// Gets the serializer.
		/// </summary>
		/// <value>The serializer.</value>
		public Serializer Serializer { get; private set; }

		/// <summary>
		/// Gets the reader used while deserializing.
		/// </summary>
		/// <value>The reader.</value>
		public EventReader Reader { get; internal set; }

		internal IYamlSerializable ObjectSerializer { get; set; }


		/// <summary>
		/// The default function to read an object from the current Yaml stream.
		/// </summary>
		/// <param name="value">The value of the receiving object, may be null.</param>
		/// <param name="expectedType">The expected type.</param>
		/// <returns>System.Object.</returns>
		public ValueOutput ReadYaml(object value, Type expectedType)
		{
			var node = Reader.Parser.Current;
			try
			{
				return ObjectSerializer.ReadYaml(this, value, FindTypeDescriptor(expectedType));
			}
			catch (Exception ex)
			{
				if (ex is YamlException)
					throw;
				throw new YamlException(node.Start, node.End, "Error while deserializing node [{0}]".DoFormat(node), ex);
			}
		}

		/// <summary>
		/// Gets or sets the type of the create.
		/// </summary>
		/// <value>The type of the create.</value>
		public IObjectFactory ObjectFactory { get; set; }

		/// <summary>
		/// Gets the writer used while deserializing.
		/// </summary>
		/// <value>The writer.</value>
		public IEventEmitter Writer { get; internal set; }

        /// <summary>
        /// Gets the emitter.
        /// </summary>
        /// <value>The emitter.</value>
	    public IEmitter Emitter
	    {
	        get { return emitter; }
	        internal set { emitter = value; }
	    }

	    /// <summary>
		/// The default function to write an object to Yaml
		/// </summary>
		public void WriteYaml(object value, Type expectedType)
		{
			ObjectSerializer.WriteYaml(this, new ValueInput(value), FindTypeDescriptor(expectedType));
		}

		/// <summary>
		/// Finds the type descriptor for the specified type.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns>An instance of <see cref="ITypeDescriptor"/>.</returns>
		public ITypeDescriptor FindTypeDescriptor(Type type)
		{
			return typeDescriptorFactory.Find(type);
		}

		/// <summary>
		/// Resolves a type from the specified tag.
		/// </summary>
		/// <param name="tagName">Name of the tag.</param>
		/// <returns>Type.</returns>
		public Type TypeFromTag(string tagName)
		{
			return tagTypeRegistry.TypeFromTag(tagName);
		}
		
		/// <summary>
		/// Resolves a tag from a type
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns>The associated tag</returns>
		public string TagFromType(Type type)
		{
			return tagTypeRegistry.TagFromType(type);
		}

		/// <summary>
		/// Resolves a type from the specified typename using registered assemblies.
		/// </summary>
		/// <param name="typeFullName">Full name of the type.</param>
		/// <returns>The type of null if not found</returns>
		public Type ResolveType(string typeFullName)
		{
			return tagTypeRegistry.ResolveType(typeFullName);
		}

		/// <summary>
		/// Gets the default tag and value for the specified <see cref="Scalar" />. The default tag can be different from a actual tag of this <see cref="NodeEvent" />.
		/// </summary>
		/// <param name="scalar">The scalar event.</param>
		/// <param name="defaultTag">The default tag decoded from the scalar.</param>
		/// <param name="value">The value extracted from a scalar.</param>
		/// <returns>System.String.</returns>
		public bool TryParseScalar(Scalar scalar, out string defaultTag, out object value)
		{
			return Settings.Schema.TryParse(scalar, true, out defaultTag, out value);
		}

	    public IMappingKeyTransform KeyTransform
	    {
	        get { return keyTransform; }
	    }

        
        public bool DecodeKeyPre(object thisObject, ITypeDescriptor descriptor, string keyIn, out string keyOut)
        {
            keyOut = keyIn;
            return keyTransform != null && keyTransform.DecodePre(this, thisObject, descriptor, keyIn, out keyOut);
        }

        public void DecodeKeyPost(object thisObject, ITypeDescriptor descriptor, object key, string decodedKeyText)
        {
            if (keyTransform != null)
            {
                keyTransform.DecodePost(this, thisObject, descriptor, key, decodedKeyText);   
            }
        }

        public string EncodeKey(object thisObject, ITypeDescriptor descriptor, object key, string keyText)
        {
            return keyTransform == null ? keyText : keyTransform.Encode(this, thisObject, descriptor, key, keyText);
        }

        public Func<object, string, string> EncodeScalarKey { get; set; }

        private struct AnchorLateBinding
		{
			public AnchorLateBinding(AnchorAlias anchorAlias, Action<object> setter)
			{
				AnchorAlias = anchorAlias;
				Setter = setter;
			}

			public readonly AnchorAlias AnchorAlias;

			public readonly Action<object> Setter;
		}

		/// <summary>
		/// Gets the alias value.
		/// </summary>
		/// <param name="alias">The alias.</param>
		/// <returns>System.Object.</returns>
		/// <exception cref="System.ArgumentNullException">alias</exception>
		/// <exception cref="AnchorNotFoundException">Alias [{0}] not found.DoFormat(alias.Value)</exception>
		public object GetAliasValue(AnchorAlias alias)
		{
			if (alias == null) throw new ArgumentNullException("alias");

			// Verify that we have the anchorserializer
			var anchorSerializer = CheckAnchorSerializer();

			object value;
			if (!anchorSerializer.TryGetAliasValue(alias.Value, out value))
			{
				throw new AnchorNotFoundException(alias.Value, alias.Start, alias.End, "Alias [{0}] not found".DoFormat(alias.Value));				
			}
			return value;
		}

		/// <summary>
		/// Adds the late binding.
		/// </summary>
		/// <param name="alias">The alias.</param>
		/// <param name="setter">The setter.</param>
		/// <exception cref="System.ArgumentException">No alias found in the ValueOutput;valueResult</exception>
		public void AddAliasBinding(AnchorAlias alias, Action<object> setter)
		{
			if (alias == null) throw new ArgumentNullException("alias");
			if (setter == null) throw new ArgumentNullException("setter");

			CheckAnchorSerializer();

			anchorLateBindings.Add(new AnchorLateBinding(alias, setter));
		}

		/// <summary>
		/// Pushes a style for the next element to be emitted.
		/// </summary>
		/// <param name="style">The style.</param>
		internal void PushStyle(YamlStyle style)
		{
			styles.Push(style);
		}

		/// <summary>
		/// Pops the current style.
		/// </summary>
		/// <returns>The current style.</returns>
		internal YamlStyle PopStyle()
		{
			return styles.Count > 0 ? styles.Pop() : YamlStyle.Any;
		}

		internal string GetAnchor()
		{
			return Anchors.Count > 0 ? Anchors.Pop() : null;
		}

		internal void ResolveLateAliasBindings()
		{
			foreach (var lateBinding in anchorLateBindings)
			{
				var alias = lateBinding.AnchorAlias;
				var value = GetAliasValue(alias);
				lateBinding.Setter(value);
			}
		}

		private AnchorSerializer CheckAnchorSerializer()
		{
			// Verify that we have the anchorserializer
			var anchorSerializer = ObjectSerializer as AnchorSerializer;
			if (anchorSerializer == null)
			{
				throw new InvalidOperationException("Alias was desactivated by the settings. This method cannot be used");
			}

			return anchorSerializer;
		}
	}
}