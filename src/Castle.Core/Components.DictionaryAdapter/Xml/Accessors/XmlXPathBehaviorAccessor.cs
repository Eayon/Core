﻿// Copyright 2004-2011 Castle Project - http://www.castleproject.org/
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.f
// See the License for the specific language governing permissions and
// limitations under the License.

#if !SL3
namespace Castle.Components.DictionaryAdapter.Xml
{
	using System;
	using System.Xml.XPath;

	public class XmlXPathBehaviorAccessor : XmlAccessor, IXmlIncludedType, IXmlIncludedTypeMap,
		IConfigurable<XPathAttribute>,
		IConfigurable<XPathFunctionAttribute>
	{
		private XmlAccessor itemAccessor;
		private XmlIncludedTypeSet includedTypes;
	    private ICompiledPath path;
		private States state;

		internal static readonly XmlAccessorFactory<XmlXPathBehaviorAccessor>
			Factory = (property, context) => new XmlXPathBehaviorAccessor(property, context);

		public XmlXPathBehaviorAccessor(PropertyDescriptor property, IXmlAccessorContext context)
			: this(property.PropertyType, context) { }

	    protected XmlXPathBehaviorAccessor(Type clrType, IXmlAccessorContext context)
	        : base(clrType, context)
		{
			includedTypes = context.IncludedTypes;
		}

	    public ICompiledPath Path
	    {
	        get { return path; }
	    }

		public bool SelectsNodes
		{
			get { return path.Expression.ReturnType == XPathResultType.NodeSet; }
		}

		IXmlIncludedType IXmlIncludedTypeMap.Default
		{
			get { return this; }
		}

		public override bool IsVolatile
		{
			get { return 0 != (state & States.Volatile); }
		}

		public override void ConfigureVolatile(bool isVolatile)
		{
			if (isVolatile)
				state |= States.Volatile;
		}

		public void Configure(XPathAttribute attribute)
		{
			if (path != null)
				throw Error.AttributeConflict(null);

			path = attribute.Path;
		}

		public void Configure(XPathFunctionAttribute attribute)
		{
		}

		public override void Prepare()
		{
			path.SetContext(Context.XmlContext);
		}

		public override object GetPropertyValue(IXmlNode node, IDictionaryAdapter da, bool ifExists)
		{
			return SelectsNodes
				? base.GetPropertyValue(node, da, ifExists)
				: Evaluate(node);
		}

		public override void SetPropertyValue(IXmlNode node, object value)
		{
			if (SelectsNodes)
				base.SetPropertyValue(node, value);
			else
				throw Error.NotSupported();
		}

		private object Evaluate(IXmlNode node)
		{
			var value = node.Evaluate(path);
			return Convert.ChangeType(value, ClrType);
		}

		public override IXmlCollectionAccessor GetCollectionAccessor(Type itemType)
		{
			return itemAccessor ?? (itemAccessor = new ItemAccessor(this));
		}

		public override IXmlCursor SelectPropertyNode(IXmlNode node, bool create)
		{
			var flags = CursorFlags.AllNodes.MutableIf(create);
			return node.Select(path, this, flags);
		}

		public override IXmlCursor SelectCollectionNode(IXmlNode node, bool create)
		{
			return node.SelectSelf(ClrType);
		}

		public override IXmlCursor SelectCollectionItems(IXmlNode node, bool create)
		{
			return node.Select(path, this, CursorFlags.AllNodes.MutableIf(create) | CursorFlags.Multiple);
		}

		public bool TryGet(string xsiType, out IXmlIncludedType includedType)
		{
			if (xsiType == null || xsiType == this.XsiType)
				return Try.Success(out includedType, this);

			if (!includedTypes.TryGet(xsiType, out includedType))
				return false;

			if (!ClrType.IsAssignableFrom(includedType.ClrType))
				return Try.Failure(out includedType);

			return true;
		}

		public bool TryGet(Type clrType, out IXmlIncludedType includedType)
		{
			return clrType == this.ClrType
				? Try.Success(out includedType, this)
				: includedTypes.TryGet(clrType, out includedType);
		}

		private class ItemAccessor : XmlXPathBehaviorAccessor
		{
			public ItemAccessor(XmlXPathBehaviorAccessor parent)
				: base(parent.ClrType.GetCollectionItemType(), parent.Context)
			{
				path          = parent.path;
				includedTypes = parent.includedTypes;
			}

			public override IXmlCollectionAccessor GetCollectionAccessor(Type itemType)
			{
				return new XmlDefaultBehaviorAccessor(itemType, Context);
			}
		}

		[Flags]
		private enum States
		{
			Volatile = 0x01
		}
	}
}
#endif
