#if __ANDROID__
#region File and License Information
/*
<File>
	<License Type="BSD">
		Copyright © 2009 - 2016, Outcoder. All rights reserved.
	
		This file is part of Calcium (http://calciumsdk.net).

		Redistribution and use in source and binary forms, with or without
		modification, are permitted provided that the following conditions are met:
			* Redistributions of source code must retain the above copyright
			  notice, this list of conditions and the following disclaimer.
			* Redistributions in binary form must reproduce the above copyright
			  notice, this list of conditions and the following disclaimer in the
			  documentation and/or other materials provided with the distribution.
			* Neither the name of the <organization> nor the
			  names of its contributors may be used to endorse or promote products
			  derived from this software without specific prior written permission.

		THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
		ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
		WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
		DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
		DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
		(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
		LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
		ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
		(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
		SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
	</License>
	<Owner Name="Daniel Vaughan" Email="danielvaughan@outcoder.com" />
	<CreationDate>$CreationDate$</CreationDate>
</File>
*/
#endregion

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

using Android.Content;
using Android.Views;
using Android.Widget;
using BareMvvm.Core.Binding;

namespace BareMvvm.Core.Bindings
{
	public class BindableListAdapter<TItem> : BaseAdapter<TItem>
	{
		readonly IList<TItem> list;
		readonly int layoutId;
		readonly ObservableCollection<TItem> observableCollection;
		readonly LayoutInflater inflater;

		public BindableListAdapter(IList<TItem> list, int layoutId)
		{
			this.list = list;
			this.layoutId = layoutId;

			observableCollection = list as ObservableCollection<TItem>;
			if (observableCollection != null)
			{
				observableCollection.CollectionChanged += HandleCollectionChanged;
			}

			Context context = ApplicationContextHolder.Context;
			inflater = LayoutInflater.From(context);
		}

		void HandleCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			NotifyDataSetChanged();
		}

		public override int Count => list.Count;

		public override long GetItemId(int position)
		{
			return position;
		}

		public override TItem this[int index] => list[index];

		readonly Dictionary<View, XmlBindingApplicator> bindingsDictionary 
					= new Dictionary<View, XmlBindingApplicator>();

		public override View GetView(int position, View convertView, ViewGroup parent)
		{
			View view = convertView ?? inflater.Inflate(layoutId, parent, false);

			TItem item = this[position];

			XmlBindingApplicator applicator;
			if (!bindingsDictionary.TryGetValue(view, out applicator))
			{
				// Don't need weak references here since adapter should only create as many views
				// as needed and then start recycling them
				applicator = new XmlBindingApplicator();
				bindingsDictionary[view] = applicator;
			}
			else
			{
				applicator.Unregister();
			}

			applicator.BindingHost.DataContext = item;

			applicator.ApplyBindings(view, layoutId);

			return view;
		}

		protected override void Dispose(bool disposing)
		{
			if (observableCollection != null)
			{
				observableCollection.CollectionChanged -= HandleCollectionChanged;
			}
			
			foreach (var operation in bindingsDictionary.Values)
			{
				operation.Unregister();
			}

			bindingsDictionary.Clear();

			base.Dispose(disposing);
		}

	}
}
#endif