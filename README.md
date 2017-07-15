# UnitedByHCL
Brain controlled Artificial Intelligence Chess
<h2>Introduction</h2>
    <p>Built this game for hackathon hosted by HCL. This is a fully functional yet simple chess program that aims to help to understand how a chess engine works. This is a standard object oriented C# solution that is meant to be easier to understand. The focus has not been to make a fast and high rated chess engine. I have developed a working chess AI that plays descent good moves with code that you hopefully like to play. A few of the more specific goals have been to correctly implement Alpha Beta Pruning and Zobrist Hashing in a way that I think can be understood by the jury</p>
    <h2>Background</h2>
    <p>I have tried before (about 5 months ago) to implement a chess engine on jsfiddle in javascript using some libraries such as chess.js for movement and chessboard.js for board visualisation. This time, I decided to use the TDD test first approach. I like TDD and I think using TDD was the main reason the engine worked this time. It is very important that the rules of chess are 100% correct implemented. Equally important is that undoing moves result exactly into the previous state. I also think that TDD contributes to good code structure and a maintainable system design.</p>
    <h2>The Code</h2>
    <p>The solution consists of two main projects. The engine (<em>chess.dll</em>) and the user interface. <em>Chess.dll</em> has everything about the game, the board, the rules and the engine. It also contains all tests. I saw no reason for having unit-tests in a separate project in this implementation. This way, one can easily see what unit-tests belongs to which class.</p>
    <p>There are currently 78 tests. Most of them are very fast and code coverage is about 100%. Total number of lines of code are just under 3000, including tests and user interface. The engine and game class where most of the logic is, is less than 700 lines of code.</p>
    <p><img src="Tests.png" style="width: 600px; height: 300px" /></p>
    <p>The Chess UI is a Windows Forms application with only a few features like load, save and setting the time for computer to think.</p>
    <h2>Features</h2>
    <ul>
        <li>Iterative <a href="http://will.thimbleby.net/algorithms/doku.php?id=minimax_search_with_alpha-beta_pruning">Alpha-Beta Pruning</a>, which drastically decrease the number of positions that need to be analyzed. At depth zero, a depth two <a href="https://chessprogramming.wikispaces.com/Quiescence+Search">Quiescence Search</a> is also performed to prevent the risk of <a href="https://chessprogramming.wikispaces.com/Horizon+Effect">horizon effect</a>.</li>
        <li><a href="https://chessprogramming.wikispaces.com/Zobrist+Hashing">Zobrist Hashing</a> to create fast lookup for already evaluated positions. I keep a few million positions in a database so every position only has to be calculated once.</li>
        <li>Parallel threads to increase performance of engine if multiple cores are available.</li>
        <li>The chess board is represented by a single square[64] array. (Bit-boards are a lot faster but perhaps little bit more complicated.)</li>
        <li>Score of the position is based on material and a positional score for each piece. In the opening, a few basic opening principles give extra points. Special calculations are also performed in the end game.</li>
        <li>Draw by repetition, insufficient material and stale mate are also evaluated.</li>
        <li><i>Opening book is not yet implemented.</i></li>
    </ul>
    <h2>The Chess Engine</h2>
    <p>This is how my chess engine is implemented.</p>
    <h3>The Board</h3>
    <p>The engine must of course understand what chess is. There are types for the board, its squares, the pieces and their sub types. Also two players, black and white.</p>
    <h3>Moves</h3>
    <p>Each piece type has a move pattern. It is used to generate moves. A list of pseudo legal moves are generated for all pieces of a player. After a pseudo legal move is madem it is tested whether the own king is in check. Own king can&acute;t be in check after a move so those moves are illegal in chess. The legal moves are kept.</p>
    <h3>Evaluation</h3>
    <p>It is actually better to evaluate moves right away. That is because the search is much more effective when moves are ordered. More about that later. Evaluation gives a score. If black is better, it is positive and negative for positions in favor of white. Each piece has a value. Queen is nine. Rook five. Bishops and knight three and pawns are one. The material score is sum of blacks pieces subtracted by sum of whites. Position of pieces are also important. My chess engine evaluates:</p>
    <ul>
        <li>Control of center (d and e pawn on rank 4 for white and rank 5 for black)</li>
        <li>Rooks on open files</li>
        <li>Queen movement in the opening is bad</li>
        <li>Castling to get the king safe is good</li>
        <li>Bad knights close to the border</li>
        <li>Double pawns</li>
        <li>Moving bishops and knights once and only once in the opening is good</li>
    </ul>
    <p>Evaluation is also about the game ending and who is the winner. When a player is in check and has no legal moves, it is check mate and the score is set to a very large number if black wins and very low for white.</p>
    <p>As you probably know, a chess game can also end in draw if a player has no legal moves (Stale mate) or if a game has repeated the same position three times. The game also ends in a draw if no capture or pawn move has been made the last 50 moves.</p>
    <h3>Search</h3>
    <p>Now that I can tell bad moves from good, I can search for good moves. It is first decided at what depth a search should be performed. It starts at depth 1 White player makes his moves and between every white move black player makes his moves for every white move. Since black player will try its best to get as large score as possible, the best move for white is the move that results into a list of black response moves with the lowest max value. This min max search is also greatly improved by logic algorithm called alpha beta pruning, which cuts off many obviously bad moves.</p>
    <p><a href="http://will.thimbleby.net/algorithms/doku.php?id=minimax_search_with_alpha-beta_pruning">This is a link to a nice animation explaining Alpha Beta Pruning</a>.</p>
    <p>The search is made with increasing depth until the set time runs out. After each iteration, the moves are ordered so good moves are evaluated first.</p>
    <p>If i manage to get everything right, something very cool happens now. My program suddenly starts to show some intelligence. I probably want to make the search more efficient, searching at greater depth and improving performance. What will probably take most of the time is score evaluation, and that can be stored in memory with a <a href="https://chessprogramming.wikispaces.com/Zobrist+Hashing">Zobrist Hash Key</a> for faster lookup.</p>
    <h2>Performance</h2>
    <p>The <a href="https://chessprogramming.wikispaces.com/Zobrist+Hashing">Zobrist Hash Key</a> is stored in a memory database together with an integer containing a few data about the position. These data only have to be calculated once. Next time the same hash key shows up, the database is queried with the key.</p>
    <p>This is the data that is packed in an 32 bit integer. The data is packed to decrease size and increase access to the database using bit shifts.</p>
    <ol>
        <li>Command Number (7 bits), used for cleaning old positions not needed</li>
        <li>If the move was legal, i.e., own king in check (1 bit)</li>
        <li>Opponent king check (1 bit)</li>
        <li>The score of the board (13 bits)</li>
        <li><strike>How many recursions it took to find the score. A deeper search replaces scores that are found less deeper for the same position (5 bits)</strike><b>Updated</b></li>
        <li>Draw By Repetion, Unsufficient Material, Stale Mate and Mate (4 bits, one bit each)</li>
    </ol>
    <p><b>Update: </b><i>I realized that the depth has nothing to do with the score of the board. I removed and simplified the code and it's performance little bit better also.</i></p>
    <p>The engine analyses about 50k positions/sec on my dual core 2.7Ghz laptop. Which is mostly enough to see about five or six moves ahead in the middle game. Most average skilled chess players (like me) should have quite some difficulty beating the engine given the same time to think. When testing it in a chess.com CPU game, I estimate it has a rating around 1300. I think the best way to improve its performance would be to replace board representation with a bit board. That would increase move generation and position evaluation so even deeper searches could be performed.</p>
    <h2>What I Have Learned During this Project</h2>
    <p>It is very important to write unit tests and perhaps even write them first. In TDD, you have to have a failing test before you can write the function. This is an excellent way of assuring everything is tested.</p>
    <p>The Alpha Beta algorithm is probably the most challenging part of a chess engine. There are many pseudo code examples on the Internet. I only managed to get one of them to work. It is the one from <a href="http://will.thimbleby.net/algorithms/doku.php?id=minimax_search_with_alpha-beta_pruning">Algorithm Wiki</a>.</p>
    <p>Hashing is an important part of Chess programming. The board has to save a position score for faster access. The hash is stored in an 64 bit datatype but there are many more possible states that a chess game can be in. I find it quite remarkable that there were no hash collisions after I implemented the Zobrist hashing that very effectively spreads the risk of having hash collisions.</p>
    <p>It is important to run performance profiling. The faster the analysis becomes, more positions are analyzed every second and even some small improvements can have large effects on overall performance.</p>
    <p>Another good site for learning is the <a href="https://chessprogramming.wikispaces.com/">Chess Programming Wiki</a>.</p>
    <p>The source is hosted on Github <a href="https://github.com/shaileshgoogler/UnitedByHCL">at this location.</a></p>
    <h2>History</h2>
    <ul>
        <li>July 9, 2017 - Version 1.0</li>
    </ul>
    <p>
        On the other hand, it might be useful to setup your own start position (also new feature) when studying the code.
    </p>
    <p>Finally, thanks to my team mates and Professor who put up with me during these few weeks in the period of Hackathon.</p>
    <p>Happy code reading and waiting to get there at transford <u>your</u> chess programming learning!</p>
