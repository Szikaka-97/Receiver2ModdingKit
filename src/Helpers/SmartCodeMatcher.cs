using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace Receiver2ModdingKit.Helpers {
	/// <inheritdoc cref="CodeMatcher" />
	public class SmartCodeMatcher {
		public class Condition {

		}

		public CodeMatcher matcher;

		//
		// Summary:
		//     The current position
		//
		// Value:
		//     The index or -1 if out of bounds
		/// <inheritdoc cref="CodeMatcher.Pos" />
		public int Pos {
			get {
				return this.matcher.Pos;
			}
			set {
				this.Start();
				this.Advance(value);
			}
		}

		//
		// Summary:
		//     Gets the number of code instructions in this matcher
		//
		// Value:
		//     The count
		/// <inheritdoc cref="CodeMatcher.Length" />
		public int Length => this.matcher.Length;

		//
		// Summary:
		//     Checks whether the position of this CodeMatcher is within bounds
		//
		// Value:
		//     True if this CodeMatcher is valid
		/// <inheritdoc cref="CodeMatcher.IsValid" />
		public bool IsValid => this.matcher.IsValid;

		//
		// Summary:
		//     Checks whether the position of this CodeMatcher is outside its bounds
		//
		// Value:
		//     True if this CodeMatcher is invalid
		/// <inheritdoc cref="CodeMatcher.IsInvalid" />
		public bool IsInvalid => this.matcher.IsInvalid;

		//
		// Summary:
		//     Gets the remaining code instructions
		//
		// Value:
		//     The remaining count
		/// <inheritdoc cref="CodeMatcher.Remaining" />
		public int Remaining => this.matcher.Remaining;

		//
		// Summary:
		//     Gets the opcode at the current position
		//
		// Value:
		//     The opcode
		/// <inheritdoc cref="CodeMatcher.Opcode" />
		public ref OpCode Opcode => ref this.matcher.Opcode;

		//
		// Summary:
		//     Gets the operand at the current position
		//
		// Value:
		//     The operand
		/// <inheritdoc cref="CodeMatcher.Operand" />
		public ref object Operand => ref this.matcher.Operand;

		//
		// Summary:
		//     Gets the labels at the current position
		//
		// Value:
		//     The labels
		/// <inheritdoc cref="CodeMatcher.Labels" />
		public ref List<Label> Labels => ref this.matcher.Labels;

		//
		// Summary:
		//     Gets the exception blocks at the current position
		//
		// Value:
		//     The blocks
		/// <inheritdoc cref="CodeMatcher.Blocks" />
		public ref List<ExceptionBlock> Blocks => ref this.matcher.Blocks;

		//
		// Summary:
		//     Gets instructions at the current position
		//
		// Value:
		//     The instruction
		/// <inheritdoc cref="CodeMatcher.Instruction" />
		public CodeInstruction Instruction => this.matcher.Instruction;

		//
		// Summary:
		//     Creates an empty code matcher
		/// <inheritdoc cref="CodeMatcher.CodeMatcher())" />
		public SmartCodeMatcher() {
			this.matcher = new CodeMatcher();
		}

		//
		// Summary:
		//     Creates a code matcher from an enumeration of instructions
		//
		// Parameters:
		//   instructions:
		//     The instructions (transpiler argument)
		//
		//   generator:
		//     An optional IL generator
		/// <inheritdoc cref="CodeMatcher.CodeMatcher(IEnumerable{CodeInstruction}, ILGenerator)" />
		public SmartCodeMatcher(IEnumerable<CodeInstruction> instructions, ILGenerator generator = null) {
			this.matcher = new CodeMatcher(instructions, generator);
		}

		//
		// Summary:
		//     Makes a clone of this instruction matcher
		//
		// Returns:
		//     A copy of this matcher
		/// <inheritdoc cref="CodeMatcher.Clone" />
		public SmartCodeMatcher Clone() {
			return new SmartCodeMatcher() {
				matcher = this.matcher.Clone()
			};
		}

		//
		// Summary:
		//     Gets instructions at the current position with offset
		//
		// Parameters:
		//   offset:
		//     The offset
		//
		// Returns:
		//     The instruction
		/// <inheritdoc cref="CodeMatcher.InstructionAt" />
		public CodeInstruction InstructionAt(int offset) => this.matcher.InstructionAt(offset);

		//
		// Summary:
		//     Gets all instructions
		//
		// Returns:
		//     A list of instructions
		/// <inheritdoc cref="CodeMatcher.Instructions" />
		public List<CodeInstruction> Instructions() => this.matcher.Instructions();

		//
		// Summary:
		//     Gets all instructions as an enumeration
		//
		// Returns:
		//     A list of instructions
		/// <inheritdoc cref="CodeMatcher.InstructionEnumeration" />
		public IEnumerable<CodeInstruction> InstructionEnumeration() => this.matcher.InstructionEnumeration();

		//
		// Summary:
		//     Gets some instructions counting from current position
		//
		// Parameters:
		//   count:
		//     Number of instructions
		//
		// Returns:
		//     A list of instructions
		/// <inheritdoc cref="CodeMatcher.Instructions(int)" />
		public List<CodeInstruction> Instructions(int count) => this.matcher.Instructions(count);

		//
		// Summary:
		//     Gets all instructions within a range
		//
		// Parameters:
		//   start:
		//     The start index
		//
		//   end:
		//     The end index
		//
		// Returns:
		//     A list of instructions
		/// <inheritdoc cref="CodeMatcher.InstructionsInRange" />
		public List<CodeInstruction> InstructionsInRange(int start, int end) => this.matcher.InstructionsInRange(start, end);

		//
		// Summary:
		//     Gets all instructions within a range (relative to current position)
		//
		// Parameters:
		//   startOffset:
		//     The start offset
		//
		//   endOffset:
		//     The end offset
		//
		// Returns:
		//     A list of instructions
		/// <inheritdoc cref="CodeMatcher.InstructionsWithOffsets" />
		public List<CodeInstruction> InstructionsWithOffsets(int startOffset, int endOffset) => InstructionsInRange(Pos + startOffset, Pos + endOffset);

		//
		// Summary:
		//     Gets a list of all distinct labels
		//
		// Parameters:
		//   instructions:
		//     The instructions (transpiler argument)
		//
		// Returns:
		//     A list of Labels
		/// <inheritdoc cref="CodeMatcher.DistinctLabels" />
		public List<Label> DistinctLabels(IEnumerable<CodeInstruction> instructions) => this.matcher.DistinctLabels(instructions);

		//
		// Summary:
		//     Reports a failure
		//
		// Parameters:
		//   method:
		//     The method involved
		//
		//   logger:
		//     The logger
		//
		// Returns:
		//     True if current position is invalid and error was logged
		/// <inheritdoc cref="CodeMatcher.ReportFailure" />
		public bool ReportFailure(MethodBase method, Action<string> logger) => this.matcher.ReportFailure(method, logger);

		//
		// Summary:
		//     Throw an InvalidOperationException if current state is invalid (position out
		//     of bounds / last match failed)
		//
		// Parameters:
		//   explanation:
		//     Explanation of where/why the exception was thrown that will be added to the exception
		//     message
		//
		// Returns:
		//     The same code matcher
		/// <inheritdoc cref="CodeMatcher.ThrowIfInvalid" />
		public SmartCodeMatcher ThrowIfInvalid(string explanation) { 
			this.matcher.ThrowIfInvalid(explanation);

			return this;
		}

		//
		// Summary:
		//     Throw an InvalidOperationException if current state is invalid (position out
		//     of bounds / last match failed), or if the matches do not match at current position
		//
		//
		// Parameters:
		//   explanation:
		//     Explanation of where/why the exception was thrown that will be added to the exception
		//     message
		//
		//   matches:
		//     Some code matches
		//
		// Returns:
		//     The same code matcher
		/// <inheritdoc cref="CodeMatcher.ThrowIfNotMatch" />
		public SmartCodeMatcher ThrowIfNotMatch(string explanation, params CodeMatch[] matches) { 
			this.matcher.ThrowIfNotMatch(explanation, matches);

			return this;
		}

		//
		// Summary:
		//     Throw an InvalidOperationException if current state is invalid (position out
		//     of bounds / last match failed), or if the matches do not match at any point between
		//     current position and the end
		//
		// Parameters:
		//   explanation:
		//     Explanation of where/why the exception was thrown that will be added to the exception
		//     message
		//
		//   matches:
		//     Some code matches
		//
		// Returns:
		//     The same code matcher
		/// <inheritdoc cref="CodeMatcher.ThrowIfNotMatchForward" />
		public SmartCodeMatcher ThrowIfNotMatchForward(string explanation, params CodeMatch[] matches) {
			this.matcher.ThrowIfNotMatchForward(explanation, matches);

			return this;
		}

		//
		// Summary:
		//     Throw an InvalidOperationException if current state is invalid (position out
		//     of bounds / last match failed), or if the matches do not match at any point between
		//     current position and the start
		//
		// Parameters:
		//   explanation:
		//     Explanation of where/why the exception was thrown that will be added to the exception
		//     message
		//
		//   matches:
		//     Some code matches
		//
		// Returns:
		//     The same code matcher
		/// <inheritdoc cref="CodeMatcher.ThrowIfNotMatchBack" />
		public SmartCodeMatcher ThrowIfNotMatchBack(string explanation, params CodeMatch[] matches) {
			this.matcher.ThrowIfNotMatchBack(explanation, matches);
			
			return this;
		}

		//
		// Summary:
		//     Throw an InvalidOperationException if current state is invalid (position out
		//     of bounds / last match failed), or if the check function returns false
		//
		// Parameters:
		//   explanation:
		//     Explanation of where/why the exception was thrown that will be added to the exception
		//     message
		//
		//   stateCheckFunc:
		//     Function that checks validity of current state. If it returns false, an exception
		//     is thrown
		//
		// Returns:
		//     The same code matcher
		/// <inheritdoc cref="CodeMatcher.ThrowIfFalse" />
		public SmartCodeMatcher ThrowIfFalse(string explanation, Func<CodeMatcher, bool> stateCheckFunc) {
			this.matcher.ThrowIfFalse(explanation, stateCheckFunc);

			return this;
		}

		//
		// Summary:
		//     Sets an instruction at current position
		//
		// Parameters:
		//   instruction:
		//     The instruction to set
		//
		// Returns:
		//     The same code matcher
		/// <inheritdoc cref="CodeMatcher.SetInstruction" />
		public SmartCodeMatcher SetInstruction(CodeInstruction instruction) {
			if (this.matcher.IsValid) {
				this.matcher.SetInstruction(instruction);
			}

			return this;
		}

		//
		// Summary:
		//     Sets instruction at current position and advances
		//
		// Parameters:
		//   instruction:
		//     The instruction
		//
		// Returns:
		//     The same code matcher
		/// <inheritdoc cref="CodeMatcher.SetInstructionAndAdvance" />
		public SmartCodeMatcher SetInstructionAndAdvance(CodeInstruction instruction) {
			if (this.matcher.IsValid) {
				this.matcher.SetInstructionAndAdvance(instruction);
			}

			return this;
		}

		//
		// Summary:
		//     Sets opcode and operand at current position
		//
		// Parameters:
		//   opcode:
		//     The opcode
		//
		//   operand:
		//     The operand
		//
		// Returns:
		//     The same code matcher
		/// <inheritdoc cref="CodeMatcher.Set" />
		public SmartCodeMatcher Set(OpCode opcode, object operand) {
			if (this.matcher.IsValid) {
				this.matcher.Set(opcode, operand);
			}

			return this;
		}

		//
		// Summary:
		//     Sets opcode and operand at current position and advances
		//
		// Parameters:
		//   opcode:
		//     The opcode
		//
		//   operand:
		//     The operand
		//
		// Returns:
		//     The same code matcher
		/// <inheritdoc cref="CodeMatcher.SetAndAdvance" />
		public SmartCodeMatcher SetAndAdvance(OpCode opcode, object operand) {
			if (this.matcher.IsValid) {
				this.matcher.SetAndAdvance(opcode, operand);
			}

			return this;
		}

		//
		// Summary:
		//     Sets opcode at current position and advances
		//
		// Parameters:
		//   opcode:
		//     The opcode
		//
		// Returns:
		//     The same code matcher
		/// <inheritdoc cref="CodeMatcher.SetOpcodeAndAdvance" />
		public SmartCodeMatcher SetOpcodeAndAdvance(OpCode opcode) {
			if (this.matcher.IsValid) {
				this.matcher.SetOpcodeAndAdvance(opcode);
			}

			return this;
		}

		//
		// Summary:
		//     Sets operand at current position and advances
		//
		// Parameters:
		//   operand:
		//     The operand
		//
		// Returns:
		//     The same code matcher
		/// <inheritdoc cref="CodeMatcher.SetOperandAndAdvance" />
		public SmartCodeMatcher SetOperandAndAdvance(object operand) {
			if (this.matcher.IsValid) {
				this.matcher.SetOperandAndAdvance(operand);
			}

			return this;
		}

		//
		// Summary:
		//     Creates a label at current position
		//
		// Parameters:
		//   label:
		//     [out] The label
		//
		// Returns:
		//     The same code matcher
		/// <inheritdoc cref="CodeMatcher.CreateLabel" />
		public SmartCodeMatcher CreateLabel(out Label label) {
			if (this.matcher.IsValid) {
				matcher.CreateLabel(out label);
			}

			return this;
		}

		//
		// Summary:
		//     Creates a label at a position
		//
		// Parameters:
		//   position:
		//     The position
		//
		//   label:
		//     [out] The new label
		//
		// Returns:
		//     The same code matcher
		/// <inheritdoc cref="CodeMatcher.CreateLabelAt" />
		public SmartCodeMatcher CreateLabelAt(int position, out Label label) {
			this.matcher.CreateLabelAt(position, out label);

			return this;
		}

		//
		// Summary:
		//     Adds an enumeration of labels to current position
		//
		// Parameters:
		//   labels:
		//     The labels
		//
		// Returns:
		//     The same code matcher
		/// <inheritdoc cref="CodeMatcher.AddLabels" />
		public SmartCodeMatcher AddLabels(IEnumerable<Label> labels) {
			if (this.matcher.IsValid) {
				this.matcher.AddLabels(labels);
			}

			return this;
		}

		//
		// Summary:
		//     Adds an enumeration of labels at a position
		//
		// Parameters:
		//   position:
		//     The position
		//
		//   labels:
		//     The labels
		//
		// Returns:
		//     The same code matcher
		/// <inheritdoc cref="CodeMatcher.AddLabelsAt" />
		public SmartCodeMatcher AddLabelsAt(int position, IEnumerable<Label> labels) {
			this.matcher.AddLabelsAt(position, labels);
			
			return this;
		}

		//
		// Summary:
		//     Sets jump to
		//
		// Parameters:
		//   opcode:
		//     Branch instruction
		//
		//   destination:
		//     Destination for the jump
		//
		//   label:
		//     [out] The created label
		//
		// Returns:
		//     The same code matcher
		/// <inheritdoc cref="CodeMatcher.SetJumpTo" />
		public SmartCodeMatcher SetJumpTo(OpCode opcode, int destination, out Label label) {
			if (this.matcher.IsValid) {
				this.matcher.SetJumpTo(opcode, destination, out label);
			}

			return this;
		}

		//
		// Summary:
		//     Inserts an instruction
		//
		// Parameters:
		//   instructions:
		//     The instructions
		//
		// Returns:
		//     The same code matcher
		/// <inheritdoc cref="CodeMatcher.Insert" />
		public SmartCodeMatcher Insert(OpCode opcode, object operand = null) {
			if (this.matcher.IsValid) {
				this.matcher.Insert(new CodeInstruction(opcode, operand));
			}

			return this;
		}

		//
		// Summary:
		//     Inserts some instructions
		//
		// Parameters:
		//   instructions:
		//     The instructions
		//
		// Returns:
		//     The same code matcher
		/// <inheritdoc cref="CodeMatcher.Insert" />
		public SmartCodeMatcher Insert(params CodeInstruction[] instructions) {
			if (this.matcher.IsValid) {
				this.matcher.Insert(instructions);
			}

			return this;
		}

		//
		// Summary:
		//     Inserts an enumeration of instructions
		//
		// Parameters:
		//   instructions:
		//     The instructions
		//
		// Returns:
		//     The same code matcher
		/// <inheritdoc cref="CodeMatcher.Insert" />
		public SmartCodeMatcher Insert(IEnumerable<CodeInstruction> instructions) {
			if (this.matcher.IsValid) {
				this.matcher.Insert(instructions);
			}

			return this;
		}

		//
		// Summary:
		//     Inserts a branch
		//
		// Parameters:
		//   opcode:
		//     The branch opcode
		//
		//   destination:
		//     Branch destination
		//
		// Returns:
		//     The same code matcher
		/// <inheritdoc cref="CodeMatcher.InsertBranch" />
		public SmartCodeMatcher InsertBranch(OpCode opcode, int destination) {
			if (this.matcher.IsValid) {
				this.matcher.InsertBranch(opcode, destination);
			}

			return this;
		}

		//
		// Summary:
		//     Inserts an instruction and advances the position
		//
		// Parameters:
		//   instructions:
		//     The instructions
		//
		// Returns:
		//     The same code matcher
		/// <inheritdoc cref="CodeMatcher.InsertAndAdvance" />
		public SmartCodeMatcher InsertAndAdvance(OpCode opcode, object operand = null) {
			if (this.matcher.IsValid) {
				this.matcher.InsertAndAdvance(new CodeInstruction(opcode, operand));
			}

			return this;
		}

		//
		// Summary:
		//     Inserts some instructions and advances the position
		//
		// Parameters:
		//   instructions:
		//     The instructions
		//
		// Returns:
		//     The same code matcher
		/// <inheritdoc cref="CodeMatcher.InsertAndAdvance" />
		public SmartCodeMatcher InsertAndAdvance(params CodeInstruction[] instructions) {
			if (this.matcher.IsValid) {
				this.matcher.InsertAndAdvance(instructions);
			}

			return this;
		}

		//
		// Summary:
		//     Inserts an enumeration of instructions and advances the position
		//
		// Parameters:
		//   instructions:
		//     The instructions
		//
		// Returns:
		//     The same code matcher
		/// <inheritdoc cref="CodeMatcher.InsertAndAdvance" />
		public SmartCodeMatcher InsertAndAdvance(IEnumerable<CodeInstruction> instructions) {
			if (this.matcher.IsValid) {
				this.matcher.InsertAndAdvance(instructions);
			}

			return this;
		}

		//
		// Summary:
		//     Inserts a branch and advances the position
		//
		// Parameters:
		//   opcode:
		//     The branch opcode
		//
		//   destination:
		//     Branch destination
		//
		// Returns:
		//     The same code matcher
		/// <inheritdoc cref="CodeMatcher.InsertBranchAndAdvance" />
		public SmartCodeMatcher InsertBranchAndAdvance(OpCode opcode, int destination) {
			if (this.matcher.IsValid) {
				this.matcher.InsertBranchAndAdvance(opcode, destination);
			}

			return this;
		}

		//
		// Summary:
		//     Removes current instruction
		//
		// Returns:
		//     The same code matcher
		/// <inheritdoc cref="CodeMatcher.RemoveInstruction" />
		public SmartCodeMatcher RemoveInstruction() {
			if (this.matcher.IsValid) {
				this.matcher.RemoveInstruction();
			}

			return this;
		}

		//
		// Summary:
		//     Removes some instruction from current position by count
		//
		// Parameters:
		//   count:
		//     Number of instructions
		//
		// Returns:
		//     The same code matcher
		/// <inheritdoc cref="CodeMatcher.RemoveInstructions" />
		public SmartCodeMatcher RemoveInstructions(int count) {
			if (this.matcher.IsValid) {
				this.matcher.RemoveInstructions(count);
			}

			return this;
		}

		//
		// Summary:
		//     Removes the instructions in a range
		//
		// Parameters:
		//   start:
		//     The start
		//
		//   end:
		//     The end
		//
		// Returns:
		//     The same code matcher
		/// <inheritdoc cref="CodeMatcher.RemoveInstructionsInRange" />
		public SmartCodeMatcher RemoveInstructionsInRange(int start, int end) {
			this.matcher.RemoveInstructionsInRange(start, end);

			return this;
		}

		//
		// Summary:
		//     Removes the instructions in a offset range
		//
		// Parameters:
		//   startOffset:
		//     The start offset
		//
		//   endOffset:
		//     The end offset
		//
		// Returns:
		//     The same code matcher
		/// <inheritdoc cref="CodeMatcher.RemoveInstructionsWithOffsets" />
		public SmartCodeMatcher RemoveInstructionsWithOffsets(int startOffset, int endOffset) {
			if (this.matcher.IsValid) {
				this.matcher.RemoveInstructionsWithOffsets(startOffset, endOffset);
			}

			return this;
		}

		//
		// Summary:
		//     Advances the current position
		//
		// Parameters:
		//   offset:
		//     The offset
		//
		// Returns:
		//     The same code matcher
		/// <inheritdoc cref="CodeMatcher.Advance" />
		public SmartCodeMatcher Advance(int offset = 1) {
			if (this.matcher.IsValid) {
				this.matcher.Advance(offset);
			}

			return this;
		}

		//
		// Summary:
		//     Moves the current position to the start
		//
		// Returns:
		//     The same code matcher
		/// <inheritdoc cref="CodeMatcher.Start" />
		public SmartCodeMatcher Start() {
			this.matcher.Start();

			return this;
		}

		//
		// Summary:
		//     Moves the current position to the end
		//
		// Returns:
		//     The same code matcher
		/// <inheritdoc cref="CodeMatcher.End" />
		public SmartCodeMatcher End() {
			this.matcher.End();

			return this;
		}

		//
		// Summary:
		//     Searches forward with a predicate and advances position
		//
		// Parameters:
		//   predicate:
		//     The predicate
		//
		// Returns:
		//     The same code matcher
		/// <inheritdoc cref="CodeMatcher.SearchForward" />
		public SmartCodeMatcher SearchForward(Func<CodeInstruction, bool> predicate) {
			if (this.matcher.IsInvalid) {
				this.matcher.Start();
			}

			this.matcher.SearchForward(predicate);

			return this;
		}

		//
		// Summary:
		//     Searches backwards with a predicate and reverses position
		//
		// Parameters:
		//   predicate:
		//     The predicate
		//
		// Returns:
		//     The same code matcher
		/// <inheritdoc cref="CodeMatcher.SearchBack" />
		public SmartCodeMatcher SearchBack(Func<CodeInstruction, bool> predicate) {
			if (this.matcher.IsInvalid) {
				this.matcher.Start();
			}

			this.matcher.SearchBack(predicate);

			return this;
		}

		//
		// Summary:
		//     Matches forward and advances position
		//
		// Parameters:
		//   useEnd:
		//     True to set position to end of match, false to set it to the beginning of the
		//     match
		//
		//   matches:
		//     Some code matches
		//
		// Returns:
		//     The same code matcher
		/// <inheritdoc cref="CodeMatcher.MatchForward" />
		public SmartCodeMatcher MatchForward(bool useEnd, params CodeMatch[] matches) {
			if (this.matcher.IsInvalid) {
				this.matcher.Start();
			}

			this.matcher.MatchForward(useEnd, matches);

			return this;
		}

		//
		// Summary:
		//     Matches backwards and reverses position
		//
		// Parameters:
		//   useEnd:
		//     True to set position to end of match, false to set it to the beginning of the
		//     match
		//
		//   matches:
		//     Some code matches
		//
		// Returns:
		//     The same code matcher
		/// <inheritdoc cref="CodeMatcher.MatchBack" />
		public SmartCodeMatcher MatchBack(bool useEnd, params CodeMatch[] matches) {
			if (this.matcher.IsInvalid) {
				this.matcher.Start();
			}

			this.matcher.MatchBack(useEnd, matches);

			return this;
		}

		//
		// Summary:
		//     Matches forward and advances position to beginning of matching sequence
		//
		// Parameters:
		//   matches:
		//     Some code matches
		//
		// Returns:
		//     The same code matcher
		/// <inheritdoc cref="CodeMatcher.MatchStartForward" />
		public SmartCodeMatcher MatchStartForward(params CodeMatch[] matches) {
			if (this.matcher.IsInvalid) {
				this.matcher.Start();
			}

			this.matcher.MatchStartForward(matches);

			return this;
		}

		//
		// Summary:
		//     Matches forward and advances position to ending of matching sequence
		//
		// Parameters:
		//   matches:
		//     Some code matches
		//
		// Returns:
		//     The same code matcher
		/// <inheritdoc cref="CodeMatcher.MatchEndForward" />
		public SmartCodeMatcher MatchEndForward(params CodeMatch[] matches) {
			if (this.matcher.IsInvalid) {
				this.matcher.Start();
			}

			this.matcher.MatchEndForward(matches);

			return this;
		}

		//
		// Summary:
		//     Matches backwards and reverses position to beginning of matching sequence
		//
		// Parameters:
		//   matches:
		//     Some code matches
		//
		// Returns:
		//     The same code matcher
		/// <inheritdoc cref="CodeMatcher.MatchStartBackwards" />
		public SmartCodeMatcher MatchStartBackwards(params CodeMatch[] matches) {
			if (this.matcher.IsInvalid) {
				this.matcher.Start();
			}

			this.matcher.MatchStartBackwards(matches);

			return this;
		}

		//
		// Summary:
		//     Matches backwards and reverses position to ending of matching sequence
		//
		// Parameters:
		//   matches:
		//     Some code matches
		//
		// Returns:
		//     The same code matcher
		/// <inheritdoc cref="CodeMatcher.MatchEndBackwards" />
		public SmartCodeMatcher MatchEndBackwards(params CodeMatch[] matches) {
			if (this.matcher.IsInvalid) {
				this.matcher.Start();
			}

			this.matcher.MatchEndBackwards(matches);

			return this;
		}

		//
		// Summary:
		//     Repeats a match action until boundaries are met
		//
		// Parameters:
		//   matchAction:
		//     The match action
		//
		//   notFoundAction:
		//     An optional action that is executed when no match is found
		//
		// Returns:
		//     The same code matcher
		/// <inheritdoc cref="CodeMatcher.Repeat" />
		public SmartCodeMatcher Repeat(Action<CodeMatcher> matchAction, Action<string> notFoundAction = null) {
			if (this.matcher.IsInvalid) {
				this.matcher.Start();
			}
			
			this.matcher.Repeat(matchAction, notFoundAction);

			return this;
		}

		//
		// Summary:
		//     Gets a match by its name
		//
		// Parameters:
		//   name:
		//     The match name
		//
		// Returns:
		//     An instruction
		/// <inheritdoc cref="CodeMatcher.NamedMatch" />
		public CodeInstruction NamedMatch(string name) => this.matcher.NamedMatch(name);

		/// <inheritdoc cref="CodeMatcher.CreateBranch" />
		public SmartCodeMatcher CreateBranch(OpCode branch_opcode, CodeInstruction[] if_true, CodeInstruction[] if_false = null) {

			this.Insert(OpCodes.Nop);

			this.InsertBranchAndAdvance(branch_opcode, this.Pos);

			this.InsertAndAdvance(
				if_true
			);

			if (if_false != null) {
				this.InsertBranchAndAdvance(OpCodes.Br, this.Pos + 1);

				this.Advance(1);
				this.InsertAndAdvance(
					if_false
				);
			}

			return this;
		}

		public static implicit operator CodeMatcher(SmartCodeMatcher matcher) => matcher.matcher;
	}
}