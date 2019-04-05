using System;

namespace Motivational
{
    public class Quote
    {
        public string Text { get; set; }
        public string Source { get; set; }
    }

    public class Quotes
    {
        #region Quotes

        private readonly string[,] QUOTES =
        {
            {"Yesterday, you said tomorrow.", string.Empty},
            {"Don't compare your beginning to someone else's middle.", string.Empty},
            {"The wisest mind has something yet to learn.", string.Empty},
            {"Be the change you wish to see in the world.", "Gandhi"},
            {"When you're going through hell, keep going.", "Winston Churchill"},
            {"Don't let perfection become procrastination. Do it now.", string.Empty},
            {"Launch and learn. Everything is progress.", string.Empty},
            {"A year from now you will wish you had started today.", "Karen Lamb"},
            {"Failure is success if you learn from it.", string.Empty},
            {"If you don't like where you are, change it.", string.Empty},
            {"If it ain't fun, don't do it.", string.Empty},
            {"A wet man does not fear the rain.", string.Empty},
            {"Stay hungry; stay foolish.", string.Empty},
            {"No one saves us but ourselves. No one can and no one may. We ourselves must walk the path.", "Buddha"},
            {"Never give up. Never let things out of your control dictate who you are.", string.Empty},
            {"Be kind; everyone you meet is fighting a hard battle.", string.Empty},
            {"Impossible is just a big word thrown around by small men who find it easier to live in the world they've been given than to explore the power they have to change it.", string.Empty},
            {"People who are unable to motivate themselves must be content with mediocrity no matter how impressive their other talents.", "Andrew Carnegie"},
            {"Progress is impossible without change, and those who cannot change their minds cannot change anything.", string.Empty},
            {"Do more of what makes you happy.", string.Empty},
            {"Do a little more of what you want to do every day, until your idea becomes what's real.", string.Empty},
            {"You got this. Make it happen.", string.Empty},
            {"Don't blame others as an excuse for your not working hard enough.", string.Empty},
            {"Care about what other people think and you will always be their prisoner.", "Lao Tzu"},
            {"To escape criticism: do nothing, say nothing, be nothing.", string.Empty},
            {"The world is moving so fast that the man who says it can't be done is generally interrupted by someone doing it.", "Elbert Hubbard"},
            {"Be who you are and say what you feel, because those who mind don't matter and those who matter don't mind.", "Elbert Hubbard"},
            {"One day you will wake up and there won't be any more time to do the things you've always wanted. Do it now.", "Paulo Coelho"},
            {"Never waste a minute thinking about people you don't like.", "Dwight Eisenhower"},
            {"Never let your fear decide your fate.", string.Empty},
            {"Keep moving forward. One step at a time.", string.Empty},
            {"Life is simple. Are you happy? Yes? Keep going. No? Change something.", string.Empty},
            {"The journey of a thousand miles begins with a single step.", string.Empty},
            {"First they ignore you, then they laugh at you, then they fight you, then you win.", " Gandhi"},
            {"A man is but the product of his thoughts. What he thinks, he becomes.", "Gandhi"},
            {"Live as if you were to die tomorrow. Learn as if you were to live forever.", "Gandhi"},
            {"The future depends on what we do in the present.", "Gandhi"},
            {"I am strong because I've been weak. I am fearless because I've been afraid. I am wise, because I've been foolish.", string.Empty},
            {"Believe in yourself.", string.Empty},
            {"Lower the cost of failure.", string.Empty},
            {"Keep your goals away from the trolls.", string.Empty},
            {"Respect yourself enough to walk away from anything that no longer serves you, grows you, or makes you happy.", "Robert Tew"},
            {"Everything around you that you call life was made up by people, and you can change it.", string.Empty},
            {"In times of change, learners inherit the earth, while the learned find themselves beautifully equipped to deal with a world that no longer exists.", "Eric Hoffer"},
            {"If you fear failure, you will never go anywhere.", string.Empty},
            {"Go ahead, let them judge you.", string.Empty},
            {"The world breaks everyone and afterward many are strong at the broken places.", string.Empty},
            {"The only disability in life is a bad attitude.", string.Empty},
            {"If most of us are ashamed of shabby clothes and shoddy furniture, let us be more ashamed of shabby ideas and shoddy philosophies.", "Einstein"},
            {"It is no measure of health to be well adjusted to a profoundly sick society.", "Krishnamurti"},
            {"Think lightly of yourself and deeply of the world.", "Miyamoto Musashi"},
            {"Dude, suckin' at something is the first step to being sorta good at something.", string.Empty},
            {"As you think, so shall you become.", string.Empty},
            {"Do not wish for an easy life. Wish for the strength to endure a difficult one.", "Bruce Lee"},
            {"Showing off is the fool's idea of glory.", "Bruce Lee"},
            {"Use only that which works, and take it from any place you can find it.", "Bruce Lee"},
            {"I'm not in this world to live up to your expectations and you're not in this world to live up to mine.", "Bruce Lee"},
            {"If you spend too much time thinking about a thing, you'll never get it done.", "Bruce Lee"},
            {"Knowing is not enough, we must apply. Willing is not enough, we must do.", "Bruce Lee"},
            {"Empty your cup so that it may be filled; become devoid to gain totality.", "Bruce Lee"},
            {"It's not the daily increase but daily decrease. Hack away at the unessential.", "Bruce Lee"},
            {"Be yourself. Everyone else is already taken.", "Oscar Wilde"},
            {"Darkness cannot drive out darkness; only light can do that. Hate cannot drive out hate; only love can do that.", "MLK Jr."},
            {"Yesterday is history; tomorrow is a mystery. Today is a gift, which is why we call it the present.", "Bil Keane"},
            {"Imagination is more important than knowledge. Knowledge is limited. Imagination encircles the world.", "Einstein"},
            {"I have not failed. I've just found 10,000 ways that won't work.", "Thomas Edison"},
            {"When I let go of what I am, I become what I might be.", string.Empty},
            {"It is never too late to be what you might have been.", "George Eliot"},
            {"Always be yourself, express yourself, have faith in yourself. Do not go out and look for a successful personality and duplicate it.", "Bruce Lee"},
            {"When you are content to be simply yourself and don't compare or compete, everyone will respect you.", "Lao Tzu"},
            {"If you want to awaken all of humanity, awaken all of yourself.", "Lao Tzu"},
            {"Don't regret anything you do, because in the end it makes you who you are.", string.Empty},
            {"Tension is who you think you should be. Relaxation is who you are.", string.Empty},
            {"You are confined only by the walls you build yourself.", string.Empty},
            {"Unless you try to do something beyond what you have already mastered you will never grow.", "Ralph Waldo Emerson"},
            {"Don't think about what might go wrong, think about what could be right.", string.Empty},
            {"What the caterpillar calls the end, the rest of the world calls a butterfly.", "Lao Tzu"},
            {"To be beautiful means to be yourself. You don't need to be accepted by others. You need to be yourself.", "Thich Nhat Hanh"},
            {"Let go of those who bring you down and surround yourself with those who bring out the best in you.", string.Empty},
            {"Don't let small minds convince you that your dreams are too big.", string.Empty},
            {"If you don't like something, change it. If you can't change it, change your attitude. Don't complain.", "Mary Angelou"},
            {"You can't climb the ladder of success with your hands in your pockets.", "Arnold Schwarzenegger"},
            {"You can feel sore tomorrow or you can feel sorry tomorrow. You choose.", string.Empty},
            {"It is more important to know where you are going than to get there quickly. Do not mistake activity for achievement.", "Isocrates"},
            {"There are seven days in the week and someday isn't one of them.", string.Empty},
            {"Start where you are. Use what you can. Do what you can.", "Arthur Ashe"},
            {"Dreams don't work unless you do.", string.Empty},
            {"When you wake up in the morning you have two choices: go back to sleep, or wake up and chase those dreams.", string.Empty},
            {"Everybody comes to a point in their life when they want to quit, but it's what you do at that moment that determines who you are.", string.Empty},
            {"This is your life. Do what you love, and do it often.", string.Empty},
            {"Live your dream, and wear your passion.", string.Empty},
            {"Today I will do what others won't, so tomorrow I can do what others can't.", string.Empty},
            {"The biggest room in the world is room for improvement.", string.Empty},
            {"If people aren't laughing at your dreams, your dreams aren't big enough.", string.Empty},
            {"Never look back unless you are planning to go that way.", string.Empty},
            {"Every dream begins with a dreamer. Always remember, you have within you the strength, the patience, and the passion to reach for the stars to change the world.", string.Empty},
            {"You are awesome.", string.Empty},
            {"Simplicity is the ultimate sophistication.", string.Empty},
            {"Anyone who stops learning is old.", "Henry Ford"},
            {"The cure to boredom is curiosity.", string.Empty},
            {"Never give up on a dream just because of the time it will take to accomplish it. The time will pass anyway.", string.Empty},
            {"It's time to start living the life you've only imagined.", string.Empty},
            {"You don't have to live your life the way other people expect you to.", string.Empty},
            {"The trouble with not having a goal is that you can spend your life running up and down the field and never score.", string.Empty},
            {"To be yourself in a world that is constantly trying to make you something else is the greatest accomplishment.", string.Empty},
            {"Incredible change happens in your life when you decide to take control of what you do have power over instead of craving control over what you don't.", string.Empty},
            {"Do more with less.", string.Empty},
            {"Overthinking ruins you. Ruins the situation, twists it around, makes you worry and just makes everything much worse than it actually is.", string.Empty},
            {"Replace fear of the unknown with curiosity.", string.Empty},
            {"The surest way to find your dream job is to create it.", string.Empty},
            {"What you do today is important because you are exchanging a day of your life for it.", string.Empty},
            {"One man or woman with courage is a majority.", string.Empty},
            {"Do one thing every day that scares you.", "Eleanor Roosevelt"},
            {"Failure is simply the opportunity to begin again, this time more intelligently.", "Henry Ford"},
            {"Don't just wait for inspiration. Become it.", string.Empty},
            {"Don't limit your challenges", "challenge your limits."},
            {"When you judge others, you do not define them; you define yourself.", "Wayne Dyer"},
            {"Time you enjoy wasting is not wasted time.", string.Empty},
            {"Do small things with great love.", "Mother Teresa"},
            {"Go forth and make awesomeness.", string.Empty},
            {"Your big opportunity may be right where you are now.", "Napoleon Hill"},
            {"Life begins at the end of your comfort zone.", string.Empty},
            {"Excuses are born out of fear. Eliminate your fear and there will be no excuses.", string.Empty},
            {"Happiness is not the absence of problems, it's the ability to deal with them.", string.Empty},
            {"The problem is not the problem. The problem is your attitude about the problem.", string.Empty},
            {"You don't have to be great to start, but you have to start to be great.", string.Empty},
            {"Cherish your visions and your dreams as they are the children of your soul, the blueprints of your ultimate achievements.", string.Empty},
            {"Decide that you want it more than you are afraid of it.", string.Empty},
            {"Adventure may hurt you, but monotony will kill you.", string.Empty},
            {"Obsessed is a word that the lazy use to describe the dedicated.", string.Empty},
            {"If they can do it, so can you.", string.Empty},
            {"Success isn't about being the best. It's about always getting better.", "Behance 99U"},
            {"Success is the ability to go from failure to failure without losing your enthusiasm.", "Winston Churchill"},
            {"A pessimist sees the difficulty in every opportunity; an optimist sees the opportunity in every difficulty.", "Winston Churchill"},
            {"Failure is just practice for success.", string.Empty},
            {"Shipping beats perfection.", string.Empty},
            {"Failure is simply the opportunity to begin again. This time more intelligently.", "Henry Ford"},
            {"While we are postponing, life speeds by.", "Seneca"},
            {"It always seems impossible until it's done.", "Nelson Mandela"},
            {"Don't let the mistakes and disappointments of the past control and direct your future.", "Zig Ziglar"},
            {"It's not about where your starting point is, but your end goal and the journey that will get you there.", string.Empty},
            {"Waste no more time arguing about what a good person should be. Be one.", "Marcus Aurelius"},
            {"Life shrinks of expands in proportion to one's courage.", "Anais Nin"},
            {"Absorb what is useful. Discard what is not. Add what is uniquely your own.", "Bruce Lee"},
            {"Don't find fault. Find a remedy.", "Henry Ford"},
            {"Doubt kills more dreams than failure ever will.", "Karim Seddiki"},
            {"Don't let someone who gave up on their dreams talk you out of going after yours.", "Zig Ziglar"},
        };

        #endregion

        /// <summary>
        /// Singleton
        /// </summary>
        private static Quotes _instance;
        private Quotes() { }

        public static Quotes Instance
        {
            get
            {
                if (null == _instance)
                {
                    _instance = new Quotes();
                }

                return _instance;
            }
        }

        /// <summary>
        /// Global random generator instance
        /// </summary>
        public static Random RandomGenerator = new Random();

        public static Quote GetRandomQuote()
        {
            // Get random item record
            int quoteNumber = RandomGenerator.Next((int)(Instance.QUOTES.Length / 2)); // It needs to be modded by 2 since it is two-dimensional array

            // Return random quote
            return new Quote()
            {
                Text = Instance.QUOTES[quoteNumber, 0],
                Source = Instance.QUOTES[quoteNumber, 1],
            };
        }
    }
}
