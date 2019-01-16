using System.Collections.Generic;
using System.Threading.Tasks;
using Morestachio.Framework;

namespace Morestachio
{
	/// <summary>
	///		Defines a area that has no morestachio keywords and can be rendered as is
	/// </summary>
	public class ContentDocumentItem : DocumentItemBase
	{
		/// <inheritdoc />
		public ContentDocumentItem(string content)
		{
			Content = content;
		}

		/// <inheritdoc />
		public override async Task<IEnumerable<DocumentItemExecution>> Render(IByteCounterStream outputStream, ContextObject context,
			ScopeData scopeData)
		{
			WriteContent(outputStream, Content, context);
			await Task.CompletedTask;
			return Childs.WithScope(context);
		}

		internal static void WriteContent(IByteCounterStream builder, string content, ContextObject context)
		{
			content = content ?? context.Options.Null;

			var sourceCount = builder.BytesWritten;

			if (context.Options.MaxSize == 0)
			{
				builder.Write(content);
				return;
			}

			if (sourceCount >= context.Options.MaxSize - 1)
			{
				builder.ReachedLimit = true;
				return;
			}
			//TODO this is a performance critical operation. As we might deal with variable-length encodings this cannot be compute initial
			var cl = context.Options.Encoding.GetByteCount(content);

			var overflow = sourceCount + cl - context.Options.MaxSize;
			if (overflow <= 0)
			{
				builder.Write(content, cl);
				return;
			}

			if (overflow < content.Length)
			{
				builder.Write(content.ToCharArray(0, (int)(cl - overflow)), cl - overflow);
			}
			else
			{
				builder.Write(content, cl);
			}
		}

		/// <summary>
		///		The content that should be rendered
		/// </summary>
		public string Content { get; private set; }
	}
}