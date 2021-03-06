using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using NHibernate.Linq.Visitors;
using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.Expressions;

namespace NHibernate.Linq.Clauses
{
	/// <summary>
	///     All joins are created as outer joins. An optimization in <see cref="WhereJoinDetector" /> finds
	///     joins that may be inner joined and calls <see cref="MakeInner" /> on them.
	///     <see cref="QueryModelVisitor" />'s <see cref="QueryModelVisitor.VisitNhJoinClause" /> will
	///     then emit the correct HQL join.
	/// </summary>
	public class NhJoinClause : NhClauseBase, IFromClause, IBodyClause
	{
		Expression _fromExpression;
		string _itemName;
		System.Type _itemType;

		public NhJoinClause(string itemName, System.Type itemType, Expression fromExpression)
			: this(itemName, itemType, fromExpression, Array.Empty<NhWithClause>())
		{
		}

		/// <summary>
		///     Initializes a new instance of the <see cref="T:NHibernate.Linq.Clauses.NhJoinClause" /> class.
		/// </summary>
		/// <param name="itemName">A name describing the items generated by the from clause.</param>
		/// <param name="itemType">The type of the items generated by the from clause.</param>
		/// <param name="fromExpression">
		///     The <see cref="T:System.Linq.Expressions.Expression" /> generating data items for this
		///     from clause.
		/// </param>
		/// <param name="restrictions"></param>
		public NhJoinClause(string itemName, System.Type itemType, Expression fromExpression, IEnumerable<NhWithClause> restrictions)
		{
			if (string.IsNullOrEmpty(itemName)) throw new ArgumentException("Value cannot be null or empty.", nameof(itemName));
			if (itemType == null) throw new ArgumentNullException(nameof(itemType));
			if (fromExpression == null) throw new ArgumentNullException(nameof(fromExpression));

			_itemName = itemName;
			_itemType = itemType;
			_fromExpression = fromExpression;

			Restrictions = new ObservableCollection<NhWithClause>(restrictions);
			IsInner = false;
		}

		public ObservableCollection<NhWithClause> Restrictions { get; }

		public bool IsInner { get; private set; }

		public void TransformExpressions(Func<Expression, Expression> transformation)
		{
			if (transformation == null) throw new ArgumentNullException(nameof(transformation));
			foreach (var withClause in Restrictions)
				withClause.TransformExpressions(transformation);
			FromExpression = transformation(FromExpression);
		}

		/// <summary>
		///     Accepts the specified visitor by calling its
		///     <see
		///         cref="M:Remotion.Linq.IQueryModelVisitor.VisitNhJoinClause(NHibernate.Linq.Clauses.NhJoinClause,Remotion.Linq.QueryModel,System.Int32)" />
		///     method.
		/// </summary>
		/// <param name="visitor">The visitor to accept.</param>
		/// <param name="queryModel">The query model in whose context this clause is visited.</param>
		/// <param name="index">
		///     The index of this clause in the <paramref name="queryModel" />'s
		///     <see cref="P:Remotion.Linq.QueryModel.BodyClauses" /> collection.
		/// </param>
		protected override void Accept(INhQueryModelVisitor visitor, QueryModel queryModel, int index)
		{
			visitor.VisitNhJoinClause(this, queryModel, index);
		}

		IBodyClause IBodyClause.Clone(CloneContext cloneContext)
		{
			return Clone(cloneContext);
		}

		/// <summary>
		///     Gets or sets a name describing the items generated by this from clause.
		/// </summary>
		/// <remarks>
		///     Item names are inferred when a query expression is parsed, and they usually correspond to the variable names
		///     present in that expression.
		///     However, note that names are not necessarily unique within a <see cref="T:Remotion.Linq.QueryModel" />. Use names
		///     only for readability and debugging, not for
		///     uniquely identifying <see cref="T:Remotion.Linq.Clauses.IQuerySource" /> objects. To match an
		///     <see cref="T:Remotion.Linq.Clauses.IQuerySource" /> with its references, use the
		///     <see cref="P:Remotion.Linq.Clauses.Expressions.QuerySourceReferenceExpression.ReferencedQuerySource" /> property
		///     rather than the <see cref="P:NHibernate.Linq.Clauses.NhJoinClause.ItemName" />.
		/// </remarks>
		public string ItemName
		{
			get { return _itemName; }
			set
			{
				if (string.IsNullOrEmpty(value)) throw new ArgumentException("Value cannot be null or empty.", nameof(value));
				_itemName = value;
			}
		}

		/// <summary>
		///     Gets or sets the type of the items generated by this from clause.
		/// </summary>
		/// <note type="warning">
		///     Changing the <see cref="P:NHibernate.Linq.Clauses.NhJoinClause.ItemType" /> of a
		///     <see cref="T:Remotion.Linq.Clauses.IQuerySource" /> can make all
		///     <see cref="T:Remotion.Linq.Clauses.Expressions.QuerySourceReferenceExpression" /> objects that
		///     point to that <see cref="T:Remotion.Linq.Clauses.IQuerySource" /> invalid, so the property setter should be used
		///     with care.
		/// </note>
		public System.Type ItemType
		{
			get { return _itemType; }
			set
			{
				if (value == null) throw new ArgumentNullException(nameof(value));
				_itemType = value;
			}
		}

		/// <summary>
		///     The expression generating the data items for this from clause.
		/// </summary>
		public Expression FromExpression
		{
			get { return _fromExpression; }
			set
			{
				if (value == null) throw new ArgumentNullException(nameof(value));
				_fromExpression = value;
			}
		}

		public void CopyFromSource(IFromClause source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			FromExpression = source.FromExpression;
			ItemName = source.ItemName;
			ItemType = source.ItemType;
		}

		public NhJoinClause Clone(CloneContext cloneContext)
		{
			var joinClause = new NhJoinClause(ItemName, ItemType, FromExpression, Restrictions);
			cloneContext.QuerySourceMapping.AddMapping(this, new QuerySourceReferenceExpression(joinClause));
			return joinClause;
		}

		public void MakeInner()
		{
			IsInner = true;
		}

		public override string ToString()
		{
			return string.Format("join {0} {1} in {2}", ItemType.Name, ItemName, FromExpression);
		}
	}
}
